import { Component, ElementRef, ViewChild, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FileUploadService } from '../../services/file-upload.service';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatButton } from '@angular/material/button';
import { UploadState } from '../../models/upload-state.model';
import { ParsedWorkbook } from '../../models/parsed-sheet.model';

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
  state = signal<UploadState>({ status: 'idle' });
  uploadProgress = 0;
  isDragging = false;

  private parsedWorkbook: ParsedWorkbook | null = null;

  constructor(
    private fileUploadService: FileUploadService,
    private router: Router
  ) {}

  openFilePicker(): void {
    this.fileInputRef.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }
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
      return;
    }
    this.validateFile(file);
  }

  private validateFile(file: File): void {
    const validExtensions = ['.xlsx', '.xls'];
    const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();

    if (!validExtensions.includes(extension)) {
      this.state.set({ status: 'error', message: 'Only .xlsx and .xls files are supported.' });
      return;
    }

    if (file.size > this.maxFileSize) {
      this.state.set({ status: 'error', message: 'File exceeds the 10 MB size limit.' });
      return;
    }

    this.selectedFile = file;
    this.state.set({ status: 'selected' });
  }

  uploadFile(): void {
    if (!this.selectedFile) return;

    const formData = new FormData();
    formData.append('file', this.selectedFile, this.selectedFile.name);

    this.state.set({ status: 'uploading' });

    this.fileUploadService.UploadFile(formData).subscribe({
      next: (data) => {
        this.parsedWorkbook = data;
        this.state.set({ status: 'success', message: 'File uploaded successfully.' });
      },
      error: (err) => {
        const msg = err?.error?.message ?? 'Upload failed. Please try again.';
        this.state.set({ status: 'error', message: msg });
      },
    });
  }

  continueToMapping(): void {
    if (!this.parsedWorkbook) return;
    this.router.navigate(['/convert'], { state: this.parsedWorkbook });
  }

  reset(): void {
    this.selectedFile = null;
    this.uploadProgress = 0;
    this.parsedWorkbook = null;
    this.state.set({ status: 'idle' });
    if (this.fileInputRef) {
      this.fileInputRef.nativeElement.value = '';
    }
  }

  get formattedFileSize(): string {
    if (!this.selectedFile) return '';
    const fileSize = this.selectedFile.size;

    if (fileSize < 1024) {
      return `${fileSize} B`;
    }
    if (fileSize < 1024 * 1024) {
      return `${(fileSize / 1024).toFixed(1)} KB`;
    }
    return `${(fileSize / (1024 * 1024)).toFixed(2)} MB`;
  }
}
