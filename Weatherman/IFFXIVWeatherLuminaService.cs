// from https://github.com/karashiiro/FFXIVWeather.Lumina
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman
{
    interface IFFXIVWeatherLuminaService
    {
        /// <summary>
        ///     Returns the next <paramref name="count"/> forecast entries for the provided territory type,
        ///     at a separation defined by <paramref name="secondIncrement"/> and from the provided
        ///     initial offset in seconds.
        /// </summary>
        /// <param name="terriTypeId">The territory type to calculate a forecast for.</param>
        /// <param name="count">The number of entries to return.</param>
        /// <param name="secondIncrement">The offset in seconds between forecasts.</param>
        /// <param name="initialOffset">The offset in seconds from the current moment to begin forecasting for.</param>
        /// <returns>A list of <see cref="Weather"/>/start time tuples for the specified teritory type.</returns>
        IList<(Weather, DateTime)> GetForecast(int terriTypeId, uint count, double secondIncrement, double initialOffset);

        /// <summary>
        ///     Returns the next <paramref name="count"/> forecast entries for the provided place,
        ///     at a separation defined by <paramref name="secondIncrement"/> and from the provided
        ///     initial offset in seconds.
        /// </summary>
        /// <param name="placeName">The place to calculate a forecast for.</param>
        /// <param name="count">The number of entries to return.</param>
        /// <param name="secondIncrement">The offset in seconds between forecasts.</param>
        /// <param name="initialOffset">The offset in seconds from the current moment to begin forecasting for.</param>
        /// <returns>A list of <see cref="Weather"/>/start time tuples for the specified place.</returns>
        IList<(Weather, DateTime)> GetForecast(string placeName, uint count, double secondIncrement, double initialOffset);

        /// <summary>
        ///     Returns the next <paramref name="count"/> forecast entries for the provided territory,
        ///     at a separation defined by <paramref name="secondIncrement"/> and from the provided
        ///     initial offset in seconds.
        /// </summary>
        /// <param name="terriType">The territory to calculate a forecast for.</param>
        /// <param name="count">The number of entries to return.</param>
        /// <param name="secondIncrement">The offset in seconds between forecasts.</param>
        /// <param name="initialOffset">The offset in seconds from the current moment to begin forecasting for.</param>
        /// <returns>A list of <see cref="Weather"/>/start time tuples for the specified territory.</returns>
        IList<(Weather, DateTime)> GetForecast(TerritoryType terriType, uint count, double secondIncrement, double initialOffset);

        /// <summary>
        ///     Returns the current <see cref="Weather"/> and its start time, relative to the provided offset in seconds,
        ///     for the specified territory type.
        /// </summary>
        /// <param name="terriTypeId">The teritory type to calculate a forecast for.</param>
        /// <param name="initialOffset">The offset in seconds from the current moment to begin forecasting for.</param>
        /// <returns>A <see cref="Weather"/>/start time tuple representing the current weather in the specified territory.</returns>
        (Weather, DateTime) GetCurrentWeather(int terriTypeId, double initialOffset);

        /// <summary>
        ///     Returns the current <see cref="Weather"/> and its start time, relative to the provided offset in seconds,
        ///     for the specified place.
        /// </summary>
        /// <param name="placeName">The place to calculate a forecast for.</param>
        /// <param name="initialOffset">The offset in seconds from the current moment to begin forecasting for.</param>
        /// <returns>A <see cref="Weather"/>/start time tuple representing the current weather in the specified place.</returns>
        (Weather, DateTime) GetCurrentWeather(string placeName, double initialOffset);

        /// <summary>
        ///     Returns the current <see cref="Weather"/> and its start time, relative to the provided offset in seconds,
        ///     for the specified territory type.
        /// </summary>
        /// <param name="terriType">The teritory type to calculate a forecast for.</param>
        /// <param name="initialOffset">The offset in seconds from the current moment to begin forecasting for.</param>
        /// <returns>A <see cref="Weather"/>/start time tuple representing the current weather in the specified territory.</returns>
        (Weather, DateTime) GetCurrentWeather(TerritoryType terriType, double initialOffset);
    }
}
