import { Component, OnInit } from '@angular/core';
import {MatButtonModule} from '@angular/material/button';
import {MatCardModule} from '@angular/material/card';
import {MatIconModule} from '@angular/material/icon';
import {FormsModule} from '@angular/forms';
import {MatInputModule} from '@angular/material/input';
import {MatFormFieldModule} from '@angular/material/form-field';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

import * as Papa from "papaparse";

@Component({
  selector: 'app-maincontent',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, FormsModule, MatButtonModule, MatIconModule, CommonModule],
  templateUrl: './maincontent.html',
  styleUrls: ['./maincontent.scss']
})
export class Maincontent{
  airports: any[] = [];
  data: string | undefined; 
  constructor(private http: HttpClient) {
    this.loadCSV();
  }

  //hämtar csvc-filen. OBS BYTAS MOT HÄMTNING FRÅN BE
  loadCSV() {
    this.http.get('assets/airports.csv', { responseType: 'text' })
      .subscribe(data => {
        Papa.parse(data, {
          header: true,
          complete: (result: { data: any[]; }) => {
            const allowedTypes = ['large_airport', 'medium_airport'];
            
            this.airports = result.data
              .filter(airport => allowedTypes.includes(airport.type))
              .map(airport => ({
                ident: airport.ident,
                name: airport.name,
                municipality: airport.municipality,
                type: airport.type
              }));
            
            console.log(this.airports);
          }
        });
      });
    }

  //sköter dropdownen i inputen så att den öppnas
dropdown() {
  const dropdownEl = document.getElementById("myDropdown");
  if (dropdownEl) {
    dropdownEl.classList.toggle("show");
  }
}

    //hämtar inmatad ICAOkod samt felmeddelande vid felaktigt format
  runICAO(): void {
    const inputEl = document.getElementById("icao-input") as HTMLInputElement;
    const input = inputEl.value.trim().toUpperCase();
    const splitInput = input.split(" "); 
    const icao = splitInput[0];
    console.log(icao)

    //Felmeddelande vi felaktig inpout
    if(!/^[A-Za-z0-9]+$/.test(icao)){
      window.alert("Ogiltigt ICAO-kodformat, testa igen")
      return;
    }

    this.fetchMetarForICAO(icao);
  }

  //hämtning av koden OBS implementera från BE
  fetchMetarForICAO(icao: string) {
    console.log("Här är koden: " + icao)
    const response = "METAR ESSA 191420Z 19006KT 9999 BKN010 00/M02 Q1029 TEMPO BKN009"

    const splitMetar = response.split(" ")
    console.log(splitMetar)

   // this.http.get(`https://aviationweather.gov/api/data/metar?ids=${icao}`).subscribe(response => {console.log("SVAR från backend ", response)})
  }
}
