import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { HttpClient } from '@angular/common/http';
import * as Papa from 'papaparse';
import { MetarService } from '../services/metar.service';
import { MetarData } from '../models/metar.model';

@Component({
  selector: 'app-maincontent',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule,
    MatIconModule,
    CommonModule,
  ],
  templateUrl: './maincontent.html',
  styleUrls: ['./maincontent.scss'],
})
export class Maincontent {
  airports: any[] = [];
  metarData: MetarData | null = null;

  constructor(
    private http: HttpClient,
    private metarService: MetarService,
  ) {
    this.loadCSV();
  }

  loadCSV() {
    this.http.get('assets/airports.csv', { responseType: 'text' }).subscribe((data) => {
      Papa.parse(data, {
        header: true,
        complete: (result: { data: any[] }) => {
          const allowedTypes = ['large_airport', 'medium_airport'];

          this.airports = result.data
            .filter((airport) => allowedTypes.includes(airport.type))
            .map((airport) => ({
              ident: airport.ident,
              name: airport.name,
              municipality: airport.municipality,
              type: airport.type,
            }));
        },
      });
    });
  }

  processMetar(): void {
    const metarInput = (document.getElementById("metar-input") as HTMLInputElement).value.trim();

    if (!metarInput) {
      window.alert('Ange en ICAO-kod eller METAR-sträng');
      return;
    }

    const input = metarInput.toUpperCase();
    
    if (input.length === 4 && /^[A-Z]{4}$/.test(input)) {
      this.fetchMetarForICAO(input);
    }
    else if (input.includes(' ')) {
      const icao = input.split(' ')[0];
      if (icao.length === 4 && /^[A-Z]{4}$/.test(icao)) {
        this.fetchMetarForICAO(icao);
      } else {
        this.parseMetarString(input);
      }
    }
    else {
      this.parseMetarString(input);
    }
  }

  parseMetarString(metarString: string): void {
    window.alert('METAR-sträng parsing är inte implementerad än. Använd ICAO-sökning.');
  }

  fetchMetarForICAO(icao: string) {
    this.metarService.getMetarByIcao(icao).subscribe({
      next: (data) => {
        this.metarData = data;
      },
      error: (error) => {
        console.error('Error fetching METAR:', error);
        window.alert('Kunde inte hämta METAR-data för ' + icao);
      },
    });
  }

getWeatherIconClass(iconCode: string): string {
  const iconMap: { [key: string]: string } = {
    'wi-fog': 'wi-fog',
    'wi-snow': 'wi-snow',
    'wi-rain': 'wi-rain',
    'wi-showers': 'wi-showers',
    'wi-thunderstorm': 'wi-thunderstorm',
    'wi-cloudy': 'wi-cloudy',
    'wi-day-cloudy': 'wi-day-cloudy',
    'wi-day-sunny': 'wi-day-sunny',
    'wi-day-sunny-overcast': 'wi-day-sunny-overcast',
  };
  
  return iconMap[iconCode] || 'wi-day-sunny';
}

}
