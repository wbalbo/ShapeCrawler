﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using ShapeCrawler.Collections;
using ShapeCrawler.Exceptions;
using ShapeCrawler.Extensions;
using ShapeCrawler.Factories;
using ShapeCrawler.Settings;
using ShapeCrawler.Shared;
using ShapeCrawler.Statics;
using ShapeCrawler.Tables;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

// ReSharper disable CheckNamespace
// ReSharper disable PossibleMultipleEnumeration

namespace ShapeCrawler
{
    /// <inheritdoc cref="ITable"/>
    internal class SlideTable : SlideShape, ITable
    {
        private readonly ILocation innerTransform;
        private readonly P.GraphicFrame pGraphicFrame;
        private readonly ResettableLazy<RowCollection> rowCollection;
        private bool? hidden;
        private int id;
        private string name;

        #region Constructors

        internal SlideTable(
            OpenXmlCompositeElement pShapeTreeChild,
            ILocation innerTransform,
            ShapeContext spContext,
            SCSlide slide) : base(slide, pShapeTreeChild)
        {
            this.innerTransform = innerTransform;
            Context = spContext;
            rowCollection =
                new ResettableLazy<RowCollection>(() => RowCollection.Create(this, (P.GraphicFrame) SdkPShapeTreeChild));
            pGraphicFrame = pShapeTreeChild as P.GraphicFrame;
        }

        #endregion Constructors

        internal ShapeContext Context { get; }

        internal A.Table ATable => pGraphicFrame.GetATable();

        public void MergeCells(ITableCell inputCell1, ITableCell inputCell2) // TODO: Optimize method
        {
            SCTableCell cell1 = (SCTableCell) inputCell1;
            SCTableCell cell2 = (SCTableCell) inputCell2;
            if (CannotBeMerged(cell1, cell2))
            {
                return;
            }

            int minRowIndex = cell1.RowIndex < cell2.RowIndex ? cell1.RowIndex : cell2.RowIndex;
            int maxRowIndex = cell1.RowIndex > cell2.RowIndex ? cell1.RowIndex : cell2.RowIndex;
            int minColIndex = cell1.ColumnIndex < cell2.ColumnIndex ? cell1.ColumnIndex : cell2.ColumnIndex;
            int maxColIndex = cell1.ColumnIndex > cell2.ColumnIndex ? cell1.ColumnIndex : cell2.ColumnIndex;

            // Horizontal merging
            List<A.TableRow> aTableRowList = this.ATable.Elements<A.TableRow>().ToList();
            if (minColIndex != maxColIndex)
            {
                int horizontalMergingCount = maxColIndex - minColIndex + 1;
                for (int rowIdx = minRowIndex; rowIdx <= maxRowIndex; rowIdx++)
                {
                    A.TableCell[] rowATblCells = aTableRowList[rowIdx].Elements<A.TableCell>().ToArray();
                    A.TableCell firstMergingCell = rowATblCells[minColIndex];
                    firstMergingCell.GridSpan = new Int32Value(horizontalMergingCount);
                    Span<A.TableCell> nextMergingCells =
                        new Span<A.TableCell>(rowATblCells, minColIndex + 1, horizontalMergingCount - 1);
                    foreach (A.TableCell aTblCell in nextMergingCells)
                    {
                        aTblCell.HorizontalMerge = new BooleanValue(true);

                        MergeParagraphs(minRowIndex, minColIndex, aTblCell);
                    }
                }
            }

            // Vertical merging
            if (minRowIndex != maxRowIndex)
            {
                // Set row span value for the first cell in the merged cells
                int verticalMergingCount = maxRowIndex - minRowIndex + 1;
                IEnumerable<A.TableCell> rowSpanCells = aTableRowList[minRowIndex].Elements<A.TableCell>()
                    .Skip(minColIndex)
                    .Take(maxColIndex + 1);
                foreach (A.TableCell aTblCell in rowSpanCells)
                {
                    aTblCell.RowSpan = new Int32Value(verticalMergingCount);
                }

                // Set vertical merging flag
                foreach (A.TableRow aTblRow in aTableRowList.Skip(minRowIndex + 1).Take(maxRowIndex))
                {
                    foreach (A.TableCell aTblCell in aTblRow.Elements<A.TableCell>().Take(maxColIndex + 1))
                    {
                        aTblCell.VerticalMerge = new BooleanValue(true);

                        MergeParagraphs(minRowIndex, minColIndex, aTblCell);
                    }
                }
            }

            // Delete a:gridCol and a:tc elements if all columns are merged
            for (int colIdx = 0; colIdx < Columns.Count;)
            {
                int? gridSpan = ((SCTableCell) Rows[0].Cells[colIdx]).SdkATableCell.GridSpan?.Value;
                if (gridSpan > 1 && Rows.All(row =>
                    ((SCTableCell) row.Cells[colIdx]).SdkATableCell.GridSpan?.Value == gridSpan))
                {
                    int deleteColumnCount = gridSpan.Value - 1;

                    // Delete a:gridCol elements
                    foreach (Column column in Columns.Skip(colIdx + 1).Take(deleteColumnCount))
                    {
                        column.AGridColumn.Remove();
                        Columns[colIdx].Width += column.Width; // append width of deleting column to merged column
                    }

                    // Delete a:tc elements
                    foreach (A.TableRow aTblRow in aTableRowList)
                    {
                        IEnumerable<A.TableCell> removeCells =
                            aTblRow.Elements<A.TableCell>().Skip(colIdx).Take(deleteColumnCount);
                        foreach (A.TableCell aTblCell in removeCells)
                        {
                            aTblCell.Remove();
                        }
                    }

                    colIdx += gridSpan.Value;
                    continue;
                }

                colIdx++;
            }

            // Delete a:tr
            for (int rowIdx = 0; rowIdx < Rows.Count;)
            {
                int? rowSpan = ((SCTableCell) Rows[rowIdx].Cells[0]).SdkATableCell.RowSpan?.Value;
                if (rowSpan > 1 && Rows[rowIdx].Cells.All(c => ((SCTableCell) c).SdkATableCell.RowSpan?.Value == rowSpan))
                {
                    int deleteRowsCount = rowSpan.Value - 1;

                    // Delete a:gridCol elements
                    foreach (SCTableRow row in Rows.Skip(rowIdx + 1).Take(deleteRowsCount))
                    {
                        row.SdkATableRow.Remove();
                        Rows[rowIdx].Height += row.Height;
                    }

                    rowIdx += rowSpan.Value;
                    continue;
                }

                rowIdx++;
            }

            rowCollection.Reset();
        }

        private void MergeParagraphs(int minRowIndex, int minColIndex, A.TableCell aTblCell)
        {
            A.TextBody mergedCellTextBody = ((SCTableCell) this[minRowIndex, minColIndex]).SdkATableCell.TextBody;
            bool hasMoreOnePara = false;
            IEnumerable<A.Paragraph> aParagraphsWithARun =
                aTblCell.TextBody.Elements<A.Paragraph>().Where(p => !p.IsEmpty());
            foreach (A.Paragraph aParagraph in aParagraphsWithARun)
            {
                mergedCellTextBody.Append(aParagraph.CloneNode(true));
                hasMoreOnePara = true;
            }

            if (hasMoreOnePara)
            {
                foreach (A.Paragraph aParagraph in mergedCellTextBody.Elements<A.Paragraph>().Where(p => p.IsEmpty()))
                {
                    aParagraph.Remove();
                }
            }
        }

        #region Public Properties

        public IReadOnlyList<Column> Columns => GetColumnList(); //TODO: make lazy
        public RowCollection Rows => rowCollection.Value;
        public ITableCell this[int rowIndex, int columnIndex] => Rows[rowIndex].Cells[columnIndex];

        /// <summary>
        ///     Returns the x-coordinate of the upper-left corner of the shape.
        /// </summary>
        public long X
        {
            get => innerTransform.X;
            set => innerTransform.SetX(value);
        }

        /// <summary>
        ///     Returns the y-coordinate of the upper-left corner of the shape.
        /// </summary>
        public long Y // TODO: fix warning
        {
            get => innerTransform.Y;
            set => innerTransform.SetY(value);
        }

        /// <summary>
        ///     Returns the width of the shape.
        /// </summary>
        public long Width // TODO: fix warning
        {
            get => innerTransform.Width;
            set => innerTransform.SetWidth(value);
        }

        /// <summary>
        ///     Returns the height of the shape.
        /// </summary>
        public long Height // TODO: fix warning
        {
            get => innerTransform.Height;
            set => innerTransform.SetHeight(value);
        }

        /// <summary>
        ///     Returns an element identifier.
        /// </summary>
        public int Id // TODO: fix warning
        {
            get
            {
                InitIdHiddenName();
                return id;
            }
        }

        /// <summary>
        ///     Gets an element name.
        /// </summary>
        public string Name
        {
            get
            {
                InitIdHiddenName();
                return name;
            }
        }

        /// <summary>
        ///     Determines whether the shape is hidden.
        /// </summary>
        public bool Hidden
        {
            get
            {
                InitIdHiddenName();
                return (bool) hidden;
            }
        }

        public GeometryType GeometryType => GeometryType.Rectangle;

        public string CustomData
        {
            get => GetCustomData();
            set => SetCustomData(value);
        }

        #endregion Public Properties

        #region Private Methods

        private IReadOnlyList<Column> GetColumnList()
        {
            IEnumerable<A.GridColumn> aGridColumns = ATable.TableGrid.Elements<A.GridColumn>();
            var columnList = new List<Column>(aGridColumns.Count());
            columnList.AddRange(aGridColumns.Select(aGridColumn => new Column(aGridColumn)));

            return columnList;
        }

        private static bool CannotBeMerged(SCTableCell cell1, SCTableCell cell2)
        {
            if (cell1 == cell2)
            {
                // The cells are already merged
                return true;
            }

            return false;
        }

        private void SetCustomData(string value)
        {
            var customDataElement =
                $@"<{ConstantStrings.CustomDataElementName}>{value}</{ConstantStrings.CustomDataElementName}>";
            Context.CompositeElement.InnerXml += customDataElement;
        }

        private string GetCustomData()
        {
            var pattern = @$"<{ConstantStrings.CustomDataElementName}>(.*)<\/{ConstantStrings.CustomDataElementName}>";
            var regex = new Regex(pattern);
            var elementText = regex.Match(Context.CompositeElement.InnerXml).Groups[1];
            if (elementText.Value.Length == 0)
            {
                return null;
            }

            return elementText.Value;
        }

        private void InitIdHiddenName()
        {
            if (this.id != 0)
            {
                return;
            }

            var (id, hidden, name) = Context.CompositeElement.GetNvPrValues();
            this.id = id;
            this.hidden = hidden;
            this.name = name;
        }

        #endregion Private Methods
    }
}