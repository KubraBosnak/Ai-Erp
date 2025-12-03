import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DataViewService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5093/api/RawData';

  getData(tableName: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/${tableName}`);
  }
}

