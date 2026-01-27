import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MetarData } from '../models/metar.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class MetarService {
  private apiUrl = `${environment.apiUrl}/metar`;

  constructor(private http: HttpClient) {}

  getMetarByIcao(icaoCode: string): Observable<MetarData> {
    return this.http.get<MetarData>(`${this.apiUrl}/${icaoCode.toUpperCase()}`);
  }
}
