import { Component, ChangeDetectorRef } from '@angular/core';
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
  isLoading: boolean = false;

  constructor(
    private http: HttpClient,
    private metarService: MetarService,
    private cdr: ChangeDetectorRef,
  ) {
    this.loadCSV();
  }

  dropdown() {
    const dropdownEl = document.getElementById('myDropdown');
    if (dropdownEl) {
      dropdownEl.classList.toggle('show');
    }
  }

  loadCSV() {
    this.http.get('assets/airports.csv', { responseType: 'text' }).subscribe((data) => {
      Papa.parse(data, {
        header: true,
        complete: (result: { data: any[] }) => {
          const allowedTypes = ['large_airport', 'medium_airport'];

          this.airports = result.data
            .filter((airport) => 
              allowedTypes.includes(airport.type) && 
              airport.ident && 
              airport.ident.length === 4 && 
              /^[A-Z]{4}$/.test(airport.ident.toUpperCase())
            )
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


  runCode(): void {
    const inputEl = document.getElementById("code-input") as HTMLInputElement;
    const input = inputEl.value.trim().toUpperCase();
    const splitInput = input.split(' ');
    const code = splitInput[0];

    // Felmeddelande vid felaktig input
    if (!/^[A-Za-z0-9]+$/.test(code)) {
      window.alert('Ogiltigt kodformat, testa igen');
      return;
    }
    console.log(code)

   this.fetchMetarForICAO(code);
  }

  fetchMetarForICAO(icao: string) {
    console.log('Starting fetch for:', icao);
    this.isLoading = true;
    this.metarData = null;
    
    this.metarService.getMetarByIcao(icao).subscribe({
      next: (data) => {
        this.metarData = data;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {

        this.isLoading = false;
        this.cdr.detectChanges();
        window.alert('Kunde inte hämta METAR-data för ' + icao);
      },
      complete: () => {
        console.log('Observable completed');
      }
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
      'wi-night-clear': 'wi-night-clear',
      'wi-night-cloudy': 'wi-night-cloudy',
      'wi-windy': 'wi-windy',
      'wi-sleet': 'wi-sleet',
      'wi-hail': 'wi-hail',
      'wi-dust': 'wi-dust',
      'wi-smoke': 'wi-smoke',
    };
    
    return iconMap[iconCode] || 'wi-day-sunny';
  }

}
