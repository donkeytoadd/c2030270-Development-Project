import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import {
  ApiDetectionResult,
  JsonConversionRequest,
  JsonConversionResponse,
  MappingTemplate,
  TmForumApiDefinition,
  WorkbookConversionRequest,
  WorkbookConversionResponse
} from '../models/json-conversion.model';

@Injectable({
  providedIn: 'root'
})
export class JsonConversionService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getApiTypes(): Observable<TmForumApiDefinition[]> {
    return this.http.get<TmForumApiDefinition[]>(`${this.baseUrl}JsonConversion/ApiTypes`);
  }

  getMappingTemplates(): Observable<MappingTemplate[]> {
    return this.http.get<MappingTemplate[]>(`${this.baseUrl}JsonConversion/GetMappingTemplates`);
  }

  detectApi(fileId: string): Observable<ApiDetectionResult> {
    return this.http.get<ApiDetectionResult>(`${this.baseUrl}JsonConversion/DetectApi/${fileId}`);
  }

  convert(request: JsonConversionRequest): Observable<JsonConversionResponse> {
    return this.http.post<JsonConversionResponse>(`${this.baseUrl}JsonConversion/Convert`, request);
  }

  convertWorkbook(request: WorkbookConversionRequest): Observable<WorkbookConversionResponse> {
    return this.http.post<WorkbookConversionResponse>(`${this.baseUrl}JsonConversion/ConvertWorkbook`, request);
  }
}
