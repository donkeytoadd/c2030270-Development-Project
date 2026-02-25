import { Routes } from '@angular/router';
import {FileUploadComponent} from './components/file-upload/file-upload.component';

export const routes: Routes = [
  { path: '', redirectTo: 'upload-file', pathMatch: 'full' },
  { path: 'upload-file', component: FileUploadComponent }]
