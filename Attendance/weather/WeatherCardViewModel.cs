using Attendance.Utils;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Timers;

namespace Attendance.weather
{
    public class WeatherCardViewModel : ObservableObject
    {


        private readonly string ApiHost;
        private readonly string ApiKey;

        private readonly HttpClient _httpClient;
        private readonly System.Timers.Timer _timer;

        private string lat, lon, locationId, tz,alt;

        public string CityName { get => _cityName; set { _cityName = value; OnPropertyChanged(); } }
        public string CurrentTime { get => _currentTime; set { _currentTime = value; OnPropertyChanged(); } }
        public string CurrentDate { get => _currentDate; set { _currentDate = value; OnPropertyChanged(); } }
        public string CurrentWeekday { get => _currentWeekday; set { _currentWeekday = value; OnPropertyChanged(); } }
        public string Temperature { get => _temperature; set { _temperature = value; OnPropertyChanged(); } }
        public string WeatherText { get => _weatherText; set { _weatherText = value; OnPropertyChanged(); } }
        public string AQI { get => _aqi; set { _aqi = value; OnPropertyChanged(); } }
        public string Category { get => _category; set { _category = value; OnPropertyChanged(); } }
        public string WarningText { get => _warningText; set { _warningText = value; OnPropertyChanged(); } }
        public string MoonPhase { get => _moonPhase; set { _moonPhase = value; OnPropertyChanged(); } }
        public string Sunrise { get => _sunrise; set { _sunrise = value; OnPropertyChanged(); } }
        public string Sunset { get => _sunset; set { _sunset = value; OnPropertyChanged(); } }
        public string SolarElevationAngle { get => _solarElevationAngle; set { _solarElevationAngle = value; OnPropertyChanged(); } }
        public string PrimaryPollutant { get => _primaryPollutant; set { _primaryPollutant = value; OnPropertyChanged(); } }
        public string PrecipSummary
        {
            get => _precipSummary;
            set { _precipSummary = value; OnPropertyChanged(); }
        }

        public string MoonIconCode
        {
            get => _moonIconCode;
            set { _moonIconCode = value; OnPropertyChanged(); }
        }


        //天气预报+生活指数列表
        public ObservableCollection<WeatherInfo> CombinedInfo { get; set; } = new();

        // 降水逐分钟数据
        public ObservableCollection<PrecipitationItem> MinutelyPrecip { get; set; } = new();


        //走马灯
        public string CombinedText => string.Join("  🌈  ", CombinedInfo);

        //字体大小
        //标题
        private double _TitleFontSize = 20;
        public double TitleFontSize
        {
            get => _TitleFontSize;
            set { _TitleFontSize = value; OnPropertyChanged(); }
        }
        private double _BodyFontSize = 16;
        //正文
        public double BodyFontSize
        {
            get => _BodyFontSize;
            set { _BodyFontSize = value; OnPropertyChanged(); }
        }



        private string _sunrise, _sunset, _solarElevationAngle;


        private string _cityName, _currentTime, _currentDate, _currentWeekday;
        private string _temperature, _weatherText, _aqi, _category,_primaryPollutant;
        private string _warningText, _moonPhase,_moonIconCode, _solarRadiation;
        // 降水预报（整体描述）
        private string _precipSummary;


        public WeatherCardViewModel()
        {
            var config = ConfigHelper.InitConfig();
            ApiHost = config["WeatherApi:ApiHost"];
            ApiKey = config["WeatherApi:ApiKey"];

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler);

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (s, e) => UpdateTime();
            _timer.Start();
            UpdateTime();

            InitAsyncSafe();
        }

        private void UpdateTime()
        {
            var now = DateTime.Now;
            CurrentTime = now.ToString("HH:mm:ss");
            CurrentDate = now.ToString("yyyy-MM-dd");
            CurrentWeekday = now.ToString("dddd", new System.Globalization.CultureInfo("zh-CN"));
        }

        public async void InitAsyncSafe()
        {
            try
            {
                await InitAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("初始化失败：" + ex.Message);
            }
        }

        private async Task InitAsync()
        {
            await GetCoordinatesFromIPAsync();
            await GetLocationIdAsync();
            await GetWeatherAsync();
            await GetAirQualityAsync();
            await GetWarningAsync();
            await GetMoonAsync();
            await LoadWeatherAndLifeAsync();

            await GetSunriseSunsetAsync();
            await GetMinutelyPrecipitationAsync();
            await GetSolarElevationAngleAsync();
            // ✅ 通知滚动行为：天气数据加载完成
            Weather.WeatherReadyNotifier.ReadySignal.TrySetResult(true);
        }

        //通过IP获取经纬度
        private async Task GetCoordinatesFromIPAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("http://ip-api.com/json/");
                Debug.WriteLine("IP定位响应：" + response);
                var json = JsonDocument.Parse(response).RootElement;
                lat = json.GetProperty("lat").ToString();
                lon = json.GetProperty("lon").ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("获取经纬度失败：" + ex.Message);
            }
        }

        //通过经纬度获取城市ID
        private async Task GetLocationIdAsync()
        {
            try
            {
                string url = $"{ApiHost}/geo/v2/city/lookup?location={lon},{lat}&key={ApiKey}";
                Debug.WriteLine("城市定位请求：" + url);
                var json = JsonDocument.Parse(await _httpClient.GetStringAsync(url));
                var location = json.RootElement.GetProperty("location")[0];
                locationId = location.GetProperty("id").GetString();
                CityName = location.GetProperty("name").GetString();

                try
                {
                    // ✅ 获取时区偏移量（如 +08:00）
                    string utcOffset = location.GetProperty("utcOffset").GetString(); // 如 "+08:00"
                    //去掉冒号和加号，转换为 "0800" 或 "-0530" 格式
                    tz = utcOffset.Replace(":", "").Replace("+", ""); // "0800" 或 "-0530"
                }
                catch(Exception ex) {
                    tz = "加载失败"; // 默认值
                    Debug.WriteLine("获取时区失败：" + ex.Message);
                }
              



                // ✅ 获取海拔高度（单位：米）
                try
                {
                    string alturl = $"https://api.open-elevation.com/api/v1/lookup?locations={lat},{lon}";
                    var altjson = JsonDocument.Parse(await _httpClient.GetStringAsync(alturl));

                    // ✅ 使用 altjson 而不是 json
                    var elevation = altjson.RootElement.GetProperty("results")[0].GetProperty("elevation").GetDouble();


                    alt = ((int)Math.Round(elevation)).ToString(); // 四舍五入为整数
                }
                catch (Exception ex)
                {
                    alt = "加载失败"; // 默认值
                    Debug.WriteLine("获取海拔失败：" + ex.Message);
                    
                }

            }
            catch (Exception ex)
            {
                locationId = "未知";
                Debug.WriteLine("获取城市ID失败：" + ex.Message);
            }
        }

        //获取实时天气
        private async Task GetWeatherAsync()
        {
            try
            {
                string url = $"{ApiHost}/v7/weather/now?location={locationId}&key={ApiKey}";
                Debug.WriteLine("天气请求：" + url);
                var json = JsonDocument.Parse(await _httpClient.GetStringAsync(url));
                var now = json.RootElement.GetProperty("now");
                Temperature = now.GetProperty("temp").GetString() + "°C";
                WeatherText = now.GetProperty("text").GetString();
            }
            catch (Exception ex)
            {
                Temperature = "加载失败";
                WeatherText = "加载失败";
                Debug.WriteLine("获取天气失败：" + ex.Message);
            }
        }

        //获取空气质量
        private async Task GetAirQualityAsync()
        {
            try
            {
                string url = $"{ApiHost}/airquality/v1/current/{lat}/{lon}?key={ApiKey}";
                Debug.WriteLine("空气质量请求：" + url);
                var json = JsonDocument.Parse(await _httpClient.GetStringAsync(url));

                var index = json.RootElement.GetProperty("indexes")[0];
                AQI = index.GetProperty("aqiDisplay").GetString();
                Category = index.GetProperty("category").GetString();

                // 获取主要污染物
                if (index.TryGetProperty("primaryPollutant", out var pollutant))
                {
                    PrimaryPollutant = pollutant.GetProperty("fullName").GetString();
                }
                else
                {
                    PrimaryPollutant = "无";
                }
            }
            catch (Exception ex)
            {
                AQI = "加载失败";
                Category = "加载失败";
                PrimaryPollutant = "未知";
                Debug.WriteLine("获取空气质量失败：" + ex.Message);
            }
        }

        //获取预警信息
        private async Task GetWarningAsync()
        {
            try
            {
                string url = $"{ApiHost}/v7/warning/now?location={locationId}&key={ApiKey}";
                Debug.WriteLine("预警请求：" + url);
                var json = JsonDocument.Parse(await _httpClient.GetStringAsync(url));
                var warning = json.RootElement.GetProperty("warning");
                WarningText = warning.GetArrayLength() > 0 ? warning[0].GetProperty("text").GetString() : "无预警";
            }
            catch (Exception ex)
            {
                WarningText = "加载失败";
                Debug.WriteLine("获取预警失败：" + ex.Message);
            }
        }

        //获取月相
        private async Task GetMoonAsync()
        {
            try
            {
                string dateParam = DateTime.Now.ToString("yyyyMMdd");
                //https://km7p42awb8.re.qweatherapi.com/v7/astronomy/moon?location=114.1622,22.3211&date=20250918&key=3f61c66d957248f5a887b3512b29ea7a
                string url = $"{ApiHost}/v7/astronomy/moon?location={locationId}&date={dateParam}&key={ApiKey}";
                Debug.WriteLine("月相请求：" + url);
                
                var json = JsonDocument.Parse(await _httpClient.GetStringAsync(url));
                var moonArray = json.RootElement.GetProperty("moonPhase");

                // 获取当前小时最近的月相
                var nowHour = DateTime.Now.Hour;
                string moonName = "未知";
                string moonIcon = string.Empty;
                foreach (var item in moonArray.EnumerateArray())
                {
                    var fxTime = DateTime.Parse(item.GetProperty("fxTime").GetString());
                    if (fxTime.Hour == nowHour)
                    {
                        moonName = item.GetProperty("name").GetString();
                        moonIcon = item.GetProperty("icon").GetString();
                        break;
                    }
                }

                // 如果没找到精确小时匹配，就取第一个
                if (moonName == "未知" && moonArray.GetArrayLength() > 0)
                {
                    moonName = moonArray[0].GetProperty("name").GetString();
                    moonIcon = moonArray[0].GetProperty("icon").GetString();
                }

                MoonPhase = moonName;
                MoonIconCode = moonIcon; // 保存图标码
            }
            catch (Exception ex)
            {
                MoonPhase = "加载失败";
                Debug.WriteLine("获取月相失败：" + ex.Message);
            }
        }



        //获取天气预报+生活指数

        //获取天气预报和生活指数，并交替插入到 CombinedInfo 列表中
        private async Task LoadWeatherAndLifeAsync()
        {
            CombinedInfo.Clear();

            try
            {
                // 获取天气预报
                string forecastUrl = $"{ApiHost}/v7/weather/30d?location={locationId}&key={ApiKey}";
                var forecastJson = JsonDocument.Parse(await _httpClient.GetStringAsync(forecastUrl));
                var forecastList = forecastJson.RootElement.GetProperty("daily").EnumerateArray().ToList();

                // 获取生活指数
                string indexUrl = $"{ApiHost}/v7/indices/3d?type=3,5&location={locationId}&key={ApiKey}";
                var indexJson = JsonDocument.Parse(await _httpClient.GetStringAsync(indexUrl));
                var indexList = indexJson.RootElement.GetProperty("daily").EnumerateArray().ToList();

                // 交替插入
                int count = Math.Max(forecastList.Count, indexList.Count);
                for (int i = 0; i < count; i++)
                {
                    if (i < forecastList.Count)
                    {
                        var day = forecastList[i];
                        string date = day.GetProperty("fxDate").GetString();
                        string textDay = day.GetProperty("textDay").GetString();
                        string tempMax = day.GetProperty("tempMax").GetString();
                        string tempMin = day.GetProperty("tempMin").GetString();
                        string iconCode = day.GetProperty("iconDay").GetString();

                        CombinedInfo.Add(new WeatherInfo
                        {
                            IconKey = iconCode,
                            Category = "【天气】",
                            Description = $"{date}: {textDay} {tempMin}°C ~ {tempMax}°C"
                        });
                        //CombinedInfo.Add($"【天气】{date}: {textDay} {tempMin}°C ~ {tempMax}°C");
                    }

                    if (i < indexList.Count)
                    {
                        var index = indexList[i];
                        string name = index.GetProperty("name").GetString();
                        string category = index.GetProperty("category").GetString();
                        string text = index.GetProperty("text").GetString();
                        string lifeIconKey = name switch
                        {
                            "穿衣指数" => "Life_Dress",
                            "紫外线指数" => "Life_UV",
                            "运动指数" => "Life_Sport",
                            _ => null
                        };
                        CombinedInfo.Add(new WeatherInfo
                        {
                            IconKey = lifeIconKey,
                            Category = "【生活】",
                            Description = $"{name}：{category}，{text}"
                        });
                        //CombinedInfo.Add($"【生活】{name}：{category}，{text}");
                    }
                }

                OnPropertyChanged(nameof(CombinedInfo));
                OnPropertyChanged(nameof(CombinedText));

            }
            catch (Exception ex)
            {
                CombinedInfo.Add(new WeatherInfo { IconKey = "失败" , Category = "失败", Description = "失败" });
                Debug.WriteLine("获取天气预报或生活指数失败：" + ex.Message);
            }
        }

        //日出日落
        private async Task GetSunriseSunsetAsync()
        {
            try
            {
                string dateParam = DateTime.Now.ToString("yyyyMMdd");
                string url = $"{ApiHost}/v7/astronomy/sun?location={locationId}&date={dateParam}&key={ApiKey}";
                Debug.WriteLine("日出日落请求：" + url);
                var json = JsonDocument.Parse(await _httpClient.GetStringAsync(url));
                //获取请求的时间
                string Getsunrise = json.RootElement.GetProperty("sunrise").GetString(); ;
                string Getsunset = json.RootElement.GetProperty("sunset").GetString();
                // 直接替换 T 为空格
                string formatSunrise = Getsunrise.Replace("T", " ");
                string formatSunset = Getsunset.Replace("T", " ");
                // 结果： "2025-09-20 13:15+08:00"

                // 在时间和时区之间加空格
                Sunrise = formatSunrise.Insert(formatSunrise.LastIndexOf('+'), " ");
                Sunset = formatSunset.Insert(formatSunset.LastIndexOf('+'), " ");
                // 结果： "2025-09-20 13:15 +08:00"

            }
            catch (Exception ex)
            {
                Sunrise = "加载失败";
                Debug.WriteLine("获取日出日落失败：" + ex.Message);
            }
        }

        //太阳高度角
        private async Task GetSolarElevationAngleAsync()
        {
            try
            {
                // 当前时间拆分为日期和时间
                string date = DateTime.Now.ToString("yyyyMMdd");
                string time = DateTime.Now.ToString("HHmm");
                string url = $"{ApiHost}/v7/astronomy/solar-elevation-angle?location={lon},{lat}&date={date}&time={time}&tz={tz}&alt={alt}&key={ApiKey}";
                Debug.WriteLine("太阳高度角请求：" + url);
                var json = JsonDocument.Parse(await _httpClient.GetStringAsync(url));
                SolarElevationAngle = json.RootElement.GetProperty("solarElevationAngle").GetString() + "°";
            }
            catch (Exception ex)
            {
                SolarElevationAngle = "加载失败";
                Debug.WriteLine("获取太阳高度角失败：" + ex.Message);
            }
        }

        //降水
        private async Task GetMinutelyPrecipitationAsync()
        {
            try
            {
                string url = $"{ApiHost}/v7/minutely/5m?location={lon},{lat}&lang=zh&key={ApiKey}";
                Debug.WriteLine("分钟级降水请求：" + url);

                var json = JsonDocument.Parse(await _httpClient.GetStringAsync(url));

                // summary 字段（整体描述）
                PrecipSummary = json.RootElement.GetProperty("summary").GetString();

                // minutely 数组（未来2小时，每5分钟一条）
                var minutelyArray = json.RootElement.GetProperty("minutely");

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MinutelyPrecip.Clear();
                    foreach (var item in minutelyArray.EnumerateArray())
                    {

                        string fxTimeRaw = item.GetProperty("fxTime").GetString();
                        // 例如 "2025-09-20T13:15+08:00"

                        // 解析成 DateTimeOffset
                        DateTimeOffset dto = DateTimeOffset.Parse(fxTimeRaw);

                        // 格式化输出（不带时区）
                        string formatted = dto.ToString("yyyy-MM-dd HH:mm");
                        MinutelyPrecip.Add(new PrecipitationItem
                        {
                            FxTime = formatted,
                            Precip = item.GetProperty("precip").GetString() + " mm"
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                PrecipSummary = "降水数据加载失败";
                Debug.WriteLine("获取分钟级降水失败：" + ex.Message);
            }
        }


    }

}
