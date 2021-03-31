// from https://github.com/karashiiro/FFXIVWeather.Lumina
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;

namespace Weatherman
{
    public class FFXIVWeatherLuminaService : IFFXIVWeatherLuminaService
    {
        private const double Seconds = 1;
        private const double Minutes = 60 * Seconds;
        private const double WeatherPeriod = 23 * Minutes + 20 * Seconds;

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);

        private readonly DataManager cyalume;

        public FFXIVWeatherLuminaService(DataManager lumina)
        {
            this.cyalume = lumina;
        }

        public IList<(Weather, DateTime)> GetForecast(string placeName, uint count = 1, double secondIncrement = WeatherPeriod, double initialOffset = 0 * Minutes)
            => GetForecast(GetTerritory(placeName), count, secondIncrement, initialOffset);

        public IList<(Weather, DateTime)> GetForecast(int terriTypeId, uint count = 1, double secondIncrement = WeatherPeriod, double initialOffset = 0 * Minutes)
            => GetForecast(GetTerritory(terriTypeId), count, secondIncrement, initialOffset);

        public IList<(Weather, DateTime)> GetForecast(TerritoryType terriType, uint count = 1, double secondIncrement = WeatherPeriod, double initialOffset = 0 * Minutes)
        {
            if (count == 0) return new (Weather, DateTime)[0];

            var weatherRateIndex = GetTerritoryTypeWeatherRateIndex(terriType);

            // Initialize the return value with the current stuff
            var forecast = new List<(Weather, DateTime)> { GetCurrentWeather(terriType, initialOffset) };

            // Fill out the list
            for (var i = 1; i < count; i++)
            {
                var time = forecast[0].Item2.AddSeconds(i * secondIncrement + initialOffset);
                var weatherTarget = CalculateTarget(time);
                var weather = GetWeather(weatherRateIndex, weatherTarget);
                forecast.Add((weather, time));
            }

            return forecast;
        }

        public (Weather, DateTime) GetCurrentWeather(string placeName, double initialOffset = 0 * Minutes)
            => GetCurrentWeather(GetTerritory(placeName), initialOffset);

        public (Weather, DateTime) GetCurrentWeather(int terriTypeId, double initialOffset = 0 * Minutes)
            => GetCurrentWeather(GetTerritory(terriTypeId), initialOffset);

        public (Weather, DateTime) GetCurrentWeather(TerritoryType terriType, double initialOffset = 0 * Minutes)
        {
            var rootTime = GetCurrentWeatherRootTime(initialOffset);
            var target = CalculateTarget(rootTime);

            var weatherRateIndex = GetTerritoryTypeWeatherRateIndex(terriType);
            var weather = GetWeather(weatherRateIndex, target);

            return (weather, rootTime);
        }

        private Weather GetWeather(WeatherRate weatherRateIndex, int target)
        {
            // Based on our constraints, we know there's no null case here.
            // Every zone has at least one target at 100, and weatherTarget's domain is [0,99].
            var rateAccumulator = 0;
            var weatherId = -1;
            for (var i = 0; i < weatherRateIndex.UnkStruct0.Length; i++)
            {
                var w = weatherRateIndex.UnkStruct0[i];

                rateAccumulator += w.Rate;
                if (target < rateAccumulator)
                {
                    weatherId = w.Weather;
                    break;
                }
            }

            if (weatherId == -1)
            {
                throw new ArgumentException("No weather matching the provided parameters was found.", nameof(target));
            }

            var weather = this.cyalume.GetExcelSheet<Weather>().ToList()[weatherId];
            return weather;
        }

        private WeatherRate GetTerritoryTypeWeatherRateIndex(TerritoryType terriType)
        {
            var terriTypeWeatherRateId = terriType.WeatherRate;
            var weatherRateIndex = this.cyalume.GetExcelSheet<WeatherRate>().ToList()[terriTypeWeatherRateId];
            return weatherRateIndex;
        }

        private TerritoryType GetTerritory(string placeName)
        {
            var ciPlaceName = placeName.ToLowerInvariant();
            var terriType = this.cyalume.GetExcelSheet<TerritoryType>().FirstOrDefault(tt => ((string)tt.PlaceName.Value.Name).ToLowerInvariant() == ciPlaceName);
            if (terriType == null) throw new ArgumentException("Specified place does not exist.", nameof(placeName));
            return terriType;
        }

        private TerritoryType GetTerritory(int terriTypeId)
        {
            var terriType = this.cyalume.GetExcelSheet<TerritoryType>().FirstOrDefault(tt => tt.RowId == terriTypeId);
            if (terriType == null) throw new ArgumentException("Specified territory type does not exist.", nameof(terriTypeId));
            return terriType;
        }

        private static DateTime GetCurrentWeatherRootTime(double initialOffset)
        {
            // Calibrate the time to the beginning of the weather period
            var now = DateTime.UtcNow;
            var adjustedNow = now.AddMilliseconds(-now.Millisecond).AddSeconds(initialOffset);
            var target = CalculateTarget(adjustedNow);
            // The overhead of a binary search actually makes a linear search significantly faster here most of the time,
            // looking at ~14000 ticks vs ~18000 ticks on average. For the record, I only tested that for fun.
            var rootTime = adjustedNow;
            var anyIterations = false;
            while (CalculateTarget(rootTime) == target)
            {
                rootTime = rootTime.AddSeconds(-1);
                anyIterations = true;
            }
            // This handles the edge case where the while loop doesn't run, and more efficiently than manipulating
            // the root time in the while condition.
            if (anyIterations)
                rootTime = rootTime.AddSeconds(1);
            return rootTime;
        }

        /// <summary>
        ///     Calculate the value used for the <see cref="WeatherRateIndex"/> at a specific <see cref="DateTime" />.
        ///     This method is lifted straight from SaintCoinach.
        /// </summary>
        /// <param name="time"><see cref="DateTime"/> for which to calculate the value.</param>
        /// <returns>The value from 0..99 (inclusive) calculated based on <paramref name="time"/>.</returns>
        private static int CalculateTarget(DateTime time)
        {
            var unix = (int)(time - UnixEpoch).TotalSeconds;
            // Get Eorzea hour for weather start
            var bell = unix / 175;
            // Do the magic 'cause for calculations 16:00 is 0, 00:00 is 8 and 08:00 is 16
            var increment = ((uint)(bell + 8 - (bell % 8))) % 24;

            // Take Eorzea days since unix epoch
            var totalDays = (uint)(unix / 4200);

            var calcBase = (totalDays * 0x64) + increment;

            var step1 = (calcBase << 0xB) ^ calcBase;
            var step2 = (step1 >> 8) ^ step1;

            return (int)(step2 % 0x64);
        }
    }
}
