export interface MetarData {
  rawMetar: string;
  icaoCode: string;
  observationTime: string;
  wind?: WindInfo;
  visibility?: VisibilityInfo;
  weather?: WeatherInfo;
  clouds: CloudInfo[];
  temperature?: TemperatureInfo;
  pressure?: PressureInfo;
}

export interface WindInfo {
  direction: number;
  speed: number;
  gust?: number;
  unit: string;
}

export interface VisibilityInfo {
  value: string;
  unit: string;
}

export interface WeatherInfo {
  phenomena: string[];
  iconCode: string;
}

export interface CloudInfo {
  coverage: string;
  altitude?: number;
  type?: string;
}

export interface TemperatureInfo {
  celsius: number;
  dewpoint?: number;
}

export interface PressureInfo {
  value: number;
  unit: string;
}
