import { Component, ElementRef, ViewChild } from '@angular/core';
import { FileUploadService } from '../../services/file-upload.service';
import {MatCard, MatCardContent} from '@angular/material/card';
import {MatIcon} from '@angular/material/icon';
import {MatProgressBar} from '@angular/material/progress-bar';
import {MatButton} from '@angular/material/button';
import {UploadState} from '../../models/upload-state.model';

@Component({
  selector: 'app-file-upload',
  templateUrl: './file-upload.component.html',
  styleUrls: ['./file-upload.component.scss'],
  imports: [
    MatCard,
    MatCardContent,
    MatIcon,
    MatProgressBar,
    MatButton
  ]
})
export class FileUploadComponent {
  @ViewChild('fileInput') fileInputRef!: ElementRef<HTMLInputElement>;

  readonly acceptedTypes = '.xlsx,.xls';
  readonly maxFileSize = 10 * 1024 * 1024;

  selectedFile: File | null = null;
  state: UploadState = { status: 'idle' };
  uploadProgress = 0;
  isDragging = false;

  constructor(private fileUploadService: FileUploadService) {}

  openFilePicker(): void {
    this.fileInputRef.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.validateFile(file);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
    const file = event.dataTransfer?.files?.[0];
    if (!file) {
      return
    }
    this.validateFile(file);
  }

  private validateFile(file: File): void {
    const validExtensions = ['.xlsx', '.xls'];
    const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();

    if (!validExtensions.includes(extension)) {
      this.state = { status: 'error', message: 'Only .xlsx and .xls files are supported.' };
      return;
    }

    if (file.size > this.maxFileSize) {
      this.state = { status: 'error', message: 'File exceeds the 10 MB size limit.' };
      return;
    }

    this.selectedFile = file;
    this.state = { status: 'selected' };
  }

  uploadFile(): void {
    if (!this.selectedFile) return;

    const formData = new FormData();
    formData.append('file', this.selectedFile, this.selectedFile.name);

    this.state = { status: 'uploading' };

    this.fileUploadService.UploadFile(formData).subscribe({
      next: () => {
        this.state = { status: 'success', message: 'File uploaded successfully.' };
      },
      error: (err) => {
        const msg = err?.error?.message ?? 'Upload failed. Please try again.';
        this.state = { status: 'error', message: msg };
      },
    });
  }

  reset(): void {
    this.selectedFile = null;
    this.uploadProgress = 0;
    this.state = { status: 'idle' };
    if (this.fileInputRef) {
      this.fileInputRef.nativeElement.value = '';
    }
  }

  get formattedFileSize(): string {
    if (!this.selectedFile) return '';
    const bytes = this.selectedFile.size;

    if (bytes < 1024) {
      return `${bytes} B`;
    }
    if (bytes < 1024 * 1024) {
      return `${(bytes / 1024).toFixed(1)} KB`;
    }
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  }
}
