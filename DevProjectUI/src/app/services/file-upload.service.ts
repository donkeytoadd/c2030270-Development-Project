import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import {Observable} from 'rxjs';
import {environment} from '../environments/environment'

@Injectable({
  providedIn: 'root'
})
export class FileUploadService {

  private apiUrl = environment.apiUrl.concat("FileUpload");

  constructor(private http: HttpClient) {}

  UploadFile(formData: FormData): Observable<any>{
    return this.http.post<any>(`${this.apiUrl}/UploadFile`, formData, {reportProgress: true, observe: 'events', responseType: 'text' as 'json'})
  }
}
