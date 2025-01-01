//-----------------------------------------------------------------------
// <copyright file="CauchyLorentzDistribution.cs" company="Math.NET Project">
//    Copyright (c) 2002-2009, Christoph R�egg.
//    All Right Reserved.
// </copyright>
// <author>
//    Christoph R�egg, http://christoph.ruegg.name
// </author>
// <product>
//    Math.NET Iridium, part of the Math.NET Project.
//    http://mathnet.opensourcedotnet.info
// </product>
// <license type="opensource" name="LGPL" version="2 or later">
//    This program is free software; you can redistribute it and/or modify
//    it under the terms of the GNU Lesser General Public License as published 
//    by the Free Software Foundation; either version 2 of the License, or
//    any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public 
//    License along with this program; if not, write to the Free Software
//    Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
// </license>
// <contribution>
//    Troschuetz.Random Class Library, Stefan Trosch�tz (stefan@troschuetz.de)
// </contribution>
//-----------------------------------------------------------------------

using System;

namespace MathNet.Numerics.Distributions
{
    using RandomSources;

    /// <summary>
    /// Provides generation of Cauchy distributed random numbers.
    /// </summary>
    /// <remarks>
    /// The implementation of the <see cref="CauchyLorentzDistribution"/> type bases upon information presented on
    ///   <a href="http://en.wikipedia.org/wiki/Cauchy_distribution">Wikipedia - Cauchy distribution</a> and
    ///   <a href="http://www.xycoon.com/cauchy2p_random.htm">Xycoon - Cauchy Distribution</a>.
    /// </remarks>
    public sealed class CauchyLorentzDistribution : ContinuousDistribution
    {
        double _location;
        double _scale;

        #region Construction
        /// <summary>
        /// Initializes a new instance of the CauchyLorentzDistribution class,
        /// using a <see cref="SystemRandomSource"/> as underlying random number generator.
        /// </summary>
        public
        CauchyLorentzDistribution()
        {
            SetDistributionParameters(0.0, 1.0);
        }

        /// <summary>
        /// Initializes a new instance of the CauchyLorentzDistribution class,
        /// using the specified <see cref="RandomSource"/> as underlying random number generator.
        /// </summary>
        /// <param name="random">A <see cref="RandomSource"/> object.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="random"/> is NULL (<see langword="Nothing"/> in Visual Basic).
        /// </exception>
        public
        CauchyLorentzDistribution(RandomSource random)
            : base(random)
        {
            SetDistributionParameters(0.0, 1.0);
        }

        /// <summary>
        /// Initializes a new instance of the CauchyLorentzDistribution class,
        /// using a <see cref="SystemRandomSource"/> as underlying random number generator.
        /// </summary>
        public
        CauchyLorentzDistribution(
            double location,
            double scale)
        {
            SetDistributionParameters(location, scale);
        }
        #endregion

        #region Distribution Parameters
        /// <summary>
        /// Gets or sets the location x0 parameter.
        /// </summary>
        public double Location
        {
            get { return _location; }
            set { SetDistributionParameters(value, _scale); }
        }

        /// <summary>
        /// Gets or sets the scale gamma parameter.
        /// </summary>
        public double Scale
        {
            get { return _scale; }
            set { SetDistributionParameters(_location, value); }
        }

        /// <summary>
        /// Configure all distribution parameters.
        /// </summary>
        public
        void
        SetDistributionParameters(
            double location,
            double scale)
        {
            if(!IsValidParameterSet(location, scale))
            {
                throw new ArgumentException(Properties.LocalStrings.ArgumentParameterSetInvalid);
            }

            _location = location;
            _scale = scale;
        }

        /// <summary>
        /// Determines whether the specified parameters is valid.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if scale is greater than 0.0; otherwise, <see langword="false"/>.
        /// </returns>
        public static
        bool
        IsValidParameterSet(
            double location,
            double scale)
        {
            return scale > 0;
        }
        #endregion

        #region Distribution Properties
        /// <summary>
        /// Gets the minimum possible value of generated random numbers.
        /// </summary>
        public override double Minimum
        {
            get { return double.MinValue; }
        }

        /// <summary>
        /// Gets the maximum possible value of generated random numbers.
        /// </summary>
        public override double Maximum
        {
            get { return double.MaxValue; }
        }

        /// <summary>
        /// Gets the mean value of generated random numbers.
        /// Throws <see cref="NotSupportedException"/> since
        /// the value is not defined for this distribution.
        /// </summary>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override double Mean
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets the median of generated random numbers.
        /// </summary>
        public override double Median
        {
            get { return _location; }
        }

        /// <summary>
        /// Gets the variance of generated random numbers.
        /// Throws <see cref="NotSupportedException"/> since
        /// the value is not defined for this distribution.
        /// </summary>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override double Variance
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets the skewness of generated random numbers.
        /// Throws <see cref="NotSupportedException"/> since
        /// the value is not defined for this distribution.
        /// </summary>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override double Skewness
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Continuous probability density function (pdf) of this probability distribution.
        /// </summary>
        public override
        double
        ProbabilityDensity(double x)
        {
            double a = (x - _location) / _scale;
            return 1.0 / (Constants.Pi * _scale * (1.0 + (a * a)));
        }

        /// <summary>
        /// Continuous cumulative distribution function (cdf) of this probability distribution.
        /// </summary>
        public override
        double
        CumulativeDistribution(double x)
        {
            return (Constants.InvPi * Trig.InverseTangent((x - _location) / _scale)) + 0.5;
        }
        #endregion

        #region Generator
        /// <summary>
        /// Returns a Cauchy distributed floating point random number.
        /// </summary>
        /// <returns>A Cauchy distributed double-precision floating point number.</returns>
        public override
        double 
        NextDouble()
        {
            return _location + (_scale * Trig.Tangent(Constants.Pi * (RandomSource.NextDouble() - 0.5)));
        }
        #endregion
    }
}
