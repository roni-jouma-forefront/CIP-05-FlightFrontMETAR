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
    MatButtonModule,
    MatIconModule,
    CommonModule,
  ],
  templateUrl: './maincontent.html',
  styleUrls: ['./maincontent.scss'],
})
export class Maincontent {
  airports: any[] = [];
  data: string | undefined;
  metarData: MetarData | null = null;

  constructor(
    private http: HttpClient,
    private metarService: MetarService,
  ) {
    this.loadCSV();
  }

  //sköter dropdownen i inputen så att den
  dropdown() {
    const dropdownEl = document.getElementById('myDropdown');
    if (dropdownEl) {
      dropdownEl.classList.toggle('show');
    }
  }

  //hämtar csvc-filen. OBS ska bytas ut mot hämtning i backenden
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

          console.log(this.airports);
        },
      });
    });
  }

  //hämtar inmatad ICAOkod samt felmeddelande vid felaktigt format
  runICAO(): void {
    
    const inputEl = document.getElementById("icao-input") as HTMLInputElement;
    const input = inputEl.value.trim().toUpperCase();
    const splitInput = input.split(' ');
    const icao = splitInput[0];

    // //Felmeddelande vi felaktig inpout
    if (!/^[A-Za-z0-9]+$/.test(icao)) {
      window.alert('Ogiltigt ICAO-kodformat, testa igen');
      return;
    }
    console.log(icao)

    this.fetchMetarForICAO(icao);
  }

  fetchMetarForICAO(icao: string) {
    this.metarService.getMetarByIcao(icao).subscribe({
      next: (data) => {
        this.metarData = data;
        console.log('METAR data:', data);
      },
      error: (error) => {
        console.error('Error fetching METAR:', error);
        window.alert('Kunde inte hämta METAR-data för ' + icao);
      },
    });
  }

runMetar() {
  const inputMetar = document.getElementById("metar-input") as HTMLInputElement;
  const metar = inputMetar.value.trim().toUpperCase();
  console.log("Metar: ", metar)
}

}
