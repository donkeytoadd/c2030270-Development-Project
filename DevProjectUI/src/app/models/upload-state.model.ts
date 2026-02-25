export interface UploadState {
  status: 'idle' | 'selected' | 'parsing' | 'parsed' | 'uploading' | 'success' | 'error';
  message?: string;
}
