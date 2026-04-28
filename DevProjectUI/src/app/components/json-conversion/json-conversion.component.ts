import { Component, OnInit, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatSelect } from '@angular/material/select';
import { MatOption } from '@angular/material/core';
import { MatTooltip } from '@angular/material/tooltip';
import { JsonConversionService } from '../../services/json-conversion.service';
import { ApiDetectionCandidate, MappingTemplate, WorkbookConversionResponse } from '../../models/json-conversion.model';
import { ParsedWorkbook, ParsedSheetSummary } from '../../models/parsed-sheet.model';

type ConversionStatus = 'idle' | 'converting' | 'converted' | 'error';

@Component({
  selector: 'app-json-conversion',
  templateUrl: 'json-conversion.component.html',
  styleUrls: ['json-conversion.component.scss'],
  imports: [
    FormsModule,
    MatCard,
    MatCardContent,
    MatButton,
    MatIcon,
    MatProgressBar,
    MatFormField,
    MatLabel,
    MatSelect,
    MatOption,
    MatTooltip
  ]
})
export class JsonConversionComponent implements OnInit {
  fileId = '';
  workbookName = '';
  presentSheetNames: string[] = [];

  mappingTemplates = signal<MappingTemplate[]>([]);
  detectionCandidates = signal<ApiDetectionCandidate[]>([]);

  selectionConfirmed = signal(false);
  selectedApiId = signal('');

  status = signal<ConversionStatus>('idle');
  errorMessage = signal('');
  conversionResult = signal<WorkbookConversionResponse | null>(null);

  readonly dropdownOptions = computed<{ apiId: string; apiName: string; label: string }[]>(() => {
    const candidates = this.detectionCandidates();
    if (candidates.length > 0) {
      return candidates.map(c => ({
        apiId:   c.apiId,
        apiName: c.apiName,
        label:   `${c.apiId} – ${c.apiName} (${c.confidence}%)`
      }));
    }
    return this.mappingTemplates().map(t => ({
      apiId:   t.apiId,
      apiName: t.apiName,
      label:   `${t.apiId} – ${t.apiName}`
    }));
  });

  get selectedTemplate(): MappingTemplate | undefined {
    return this.mappingTemplates().find(t => t.apiId === this.selectedApiId());
  }

  get sheetCoverage(): { sheetName: string; present: boolean; pattern: string; isRequired: boolean }[] {
    const tmpl = this.selectedTemplate;
    if (!tmpl) return [];
    const lower = new Set(this.presentSheetNames.map(n => n.toLowerCase()));
    return tmpl.sheetMappings.map(m => ({
      sheetName:  m.sheetName,
      present:    lower.has(m.sheetName.toLowerCase()),
      pattern:    patternLabel(m.pattern),
      isRequired: m.isRequired ?? false,
    }));
  }

  get hasMissingRequiredSheets(): boolean {
    return this.sheetCoverage.some(s => s.isRequired && !s.present);
  }

  get canConvert(): boolean {
    return this.selectionConfirmed() && !this.hasMissingRequiredSheets;
  }

  get convertDisabledReason(): string {
    if (!this.selectionConfirmed()) return 'Select a TM Forum API before converting.';
    if (this.hasMissingRequiredSheets) {
      const missing = this.sheetCoverage
        .filter(s => s.isRequired && !s.present)
        .map(s => s.sheetName)
        .join(', ');
      return `Required sheet(s) missing: ${missing}`;
    }
    return '';
  }

  get detectionHint(): ApiDetectionCandidate | null {
    const top = this.detectionCandidates()[0];
    return top?.confidence > 0 ? top : null;
  }

  get formattedJson(): string {
    const result = this.conversionResult();
    if (!result) return '';
    return JSON.stringify(result.output, null, 2);
  }

  constructor(
    private router: Router,
    private jsonConversionService: JsonConversionService
  ) {}

  ngOnInit(): void {
    const state = window.history.state as ParsedWorkbook;
    if (!state?.fileId) {
      this.router.navigate(['/upload-file']);
      return;
    }

    this.fileId            = state.fileId;
    this.workbookName      = state.workbookName ?? '';
    this.presentSheetNames = (state.sheets ?? []).map((s: ParsedSheetSummary) => s.sheetName);

    this.jsonConversionService.getMappingTemplates().subscribe({
      next: (templates) => this.mappingTemplates.set(templates),
      error: () => {}
    });

    this.jsonConversionService.detectApi(this.fileId).subscribe({
      next: (result) => {
        this.detectionCandidates.set(result.candidates);
        if (result.autoSelectedApiId) {
          this.selectedApiId.set(result.autoSelectedApiId);
          this.selectionConfirmed.set(true);
        }
      },
      error: () => {}
    });
  }

  onApiSelected(apiId: string): void {
    this.selectedApiId.set(apiId);
    this.selectionConfirmed.set(true);
  }

  convert(): void {
    if (!this.canConvert) return;
    this.status.set('converting');
    this.conversionResult.set(null);

    this.jsonConversionService.convertWorkbook({
      fileId: this.fileId,
      apiId:  this.selectedApiId()
    }).subscribe({
      next: (result) => {
        this.conversionResult.set(result);
        this.status.set('converted');
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.message ?? 'Conversion failed. Please try again.');
        this.status.set('error');
      }
    });
  }

  downloadJson(): void {
    const result = this.conversionResult();
    if (!result) return;
    const filename = `${result.workbookName || result.resourceType}_${result.apiId}.json`;
    this.triggerDownload(JSON.stringify(result.output, null, 2), filename, 'application/json');
  }

  startOver(): void {
    this.router.navigate(['/upload-file']);
  }

  private triggerDownload(content: string, filename: string, mimeType: string): void {
    const blob = new Blob([content], { type: mimeType });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }
}

function patternLabel(pattern: string): string {
  switch (pattern) {
    case 'TopLevelFields': return 'Top-level fields';
    case 'FlatArray':      return 'Array';
    case 'NestedArray':    return 'Nested array';
    default:               return pattern;
  }
}
