import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5093/api/AiQuery/query';

 sendMessage(userMessage: string): Observable<any> {
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    // Backend'deki 'Question' ile birebir aynı olmalı
    const body = { Question: userMessage }; 

    return this.http.post(this.apiUrl, body, { headers });
  }
}