﻿using System;
using DocumentFormat.OpenXml;
using ShapeCrawler.Shared;
using P = DocumentFormat.OpenXml.Presentation;

namespace ShapeCrawler.Placeholders
{
    internal abstract class Placeholder : IPlaceholder
    {
        internal readonly P.PlaceholderShape SdkPPlaceholderShape;

        protected ResettableLazy<Shape> layoutReferencedShape;

        protected Placeholder(P.PlaceholderShape pPlaceholderShape)
        {
            this.SdkPPlaceholderShape = pPlaceholderShape;
        }

        /// <summary>
        ///     Gets referenced shape from lower level slide.
        /// </summary>
        protected internal Shape ReferencedShape => this.layoutReferencedShape.Value;

        public PlaceholderType Type => this.GetPlaceholderType();

        #region Private Methods

        private PlaceholderType GetPlaceholderType()
        {
            // Map SDK placeholder type into library placeholder type
            EnumValue<P.PlaceholderValues> pPlaceholderValue = this.SdkPPlaceholderShape.Type;
            if (pPlaceholderValue == null)
            {
                return PlaceholderType.Custom;
            }

            // Consider Title and Centered Title and Title as same
            if (pPlaceholderValue == P.PlaceholderValues.Title ||
                pPlaceholderValue == P.PlaceholderValues.CenteredTitle)
            {
                return PlaceholderType.Title;
            }

            // TODO: consider refactor the statement since it looks horrible
            return (PlaceholderType) Enum.Parse(typeof(PlaceholderType), pPlaceholderValue.Value.ToString());
        }

        #endregion Private Methods
    }
}