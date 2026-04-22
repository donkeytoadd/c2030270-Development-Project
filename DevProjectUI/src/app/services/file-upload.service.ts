import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { ParsedWorkbook } from '../models/parsed-sheet.model';

@Injectable({
  providedIn: 'root'
})
export class FileUploadService {

  private apiUrl = environment.apiUrl.concat("FileUpload");

  constructor(private http: HttpClient) {}

  UploadFile(formData: FormData): Observable<ParsedWorkbook> {
    return this.http.post<ParsedWorkbook>(`${this.apiUrl}/UploadFile`, formData);
  }
}
