import { Component } from '@angular/core';
import {MatButtonModule} from '@angular/material/button';
import {MatCardModule} from '@angular/material/card';
import {MatIconModule} from '@angular/material/icon';
import {FormsModule} from '@angular/forms';
import {MatInputModule} from '@angular/material/input';
import {MatFormFieldModule} from '@angular/material/form-field';

////
import { HttpClient } from '@angular/common/http';


@Component({
  selector: 'app-maincontent',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, FormsModule, MatButtonModule, MatIconModule],
  templateUrl: './maincontent.html',
  styleUrls: ['./maincontent.scss']
})
export class Maincontent {
  data: string | undefined; 
  constructor(private http: HttpClient) {}

  runICAO(): void {
    const inputEl = document.getElementById("icao-input") as HTMLInputElement;
    const icao = inputEl.value.trim().toUpperCase();

    if(!/^[A-Za-z]+$/.test(icao)){
      window.alert("Ogiltigt ICAO-kodformat, testa igen")
      return;
    }

    this.fetchMetarForICAO(icao);
  }

  fetchMetarForICAO(icao: string) {
    console.log("Här är koden: " + icao)
    const response = "METAR ESSA 191420Z 19006KT 9999 BKN010 00/M02 Q1029 TEMPO BKN009"

   // this.http.get(`https://aviationweather.gov/api/data/metar?ids=${icao}`).subscribe(response => {console.log("SVAR från backend ", response)})
  }
}
