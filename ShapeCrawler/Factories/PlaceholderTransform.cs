﻿using ShapeCrawler.Exceptions;
using ShapeCrawler.Placeholders;

namespace ShapeCrawler.Factories
{
    /// <summary>
    ///     <inheritdoc cref="ILocation" />
    /// </summary>
    internal class PlaceholderTransform : ILocation
    {
        private readonly PlaceholderLocationData _placeholderLocationData;

        #region Constructors

        public PlaceholderTransform(PlaceholderLocationData placeholderLocationData)
        {
            _placeholderLocationData = placeholderLocationData;
        }

        #endregion Constructors

        public long X => _placeholderLocationData.X;

        public long Y => _placeholderLocationData.Y;

        public long Width => _placeholderLocationData.Width;

        public long Height => _placeholderLocationData.Height;

        #region Public Methods

        public void SetX(long x)
        {
            throw new NotSupportedFeature(ExceptionMessages.PropertyCanChangedInNextVersion);
        }

        public void SetY(long y)
        {
            throw new NotSupportedFeature(ExceptionMessages.PropertyCanChangedInNextVersion);
        }

        public void SetWidth(long w)
        {
            throw new NotSupportedFeature(ExceptionMessages.PropertyCanChangedInNextVersion);
        }

        public void SetHeight(long h)
        {
            throw new NotSupportedFeature(ExceptionMessages.PropertyCanChangedInNextVersion);
        }

        #endregion Public Methods
    }
}