import { Routes } from '@angular/router';
import { FileUploadComponent } from './components/file-upload/file-upload.component';
import { JsonConversionComponent } from './components/json-conversion/json-conversion.component';

export const routes: Routes = [
  { path: '', redirectTo: 'upload-file', pathMatch: 'full' },
  { path: 'upload-file', component: FileUploadComponent },
  { path: 'convert', component: JsonConversionComponent }
];
