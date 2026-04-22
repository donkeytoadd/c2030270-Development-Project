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
import { JsonConversionService } from '../../services/json-conversion.service';
import { MappingTemplate, WorkbookConversionResponse } from '../../models/json-conversion.model';
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
    MatOption
  ]
})
export class JsonConversionComponent implements OnInit {
  fileId = '';
  workbookName = '';

  /** All sheet names present in the uploaded workbook (from the upload step). */
  presentSheetNames: string[] = [];

  mappingTemplates = signal<MappingTemplate[]>([]);
  selectedApiId = 'TMF634';

  status = signal<ConversionStatus>('idle');
  errorMessage = signal('');
  conversionResult = signal<WorkbookConversionResponse | null>(null);

  get selectedTemplate(): MappingTemplate | undefined {
    return this.mappingTemplates().find(t => t.apiId === this.selectedApiId);
  }

  /**
   * For each sheet the selected template expects, returns whether the
   * uploaded workbook actually contains it — so the user can see any gaps
   * before they hit Convert.
   */
  get sheetCoverage(): { sheetName: string; present: boolean; pattern: string }[] {
    const tmpl = this.selectedTemplate;
    if (!tmpl) return [];
    const lower = new Set(this.presentSheetNames.map(n => n.toLowerCase()));
    return tmpl.sheetMappings.map(m => ({
      sheetName: m.sheetName,
      present:   lower.has(m.sheetName.toLowerCase()),
      pattern:   patternLabel(m.pattern)
    }));
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

    this.fileId           = state.fileId;
    this.workbookName     = state.workbookName ?? '';
    this.presentSheetNames = (state.sheets ?? []).map((s: ParsedSheetSummary) => s.sheetName);

    this.jsonConversionService.getMappingTemplates().subscribe({
      next: (templates) => {
        this.mappingTemplates.set(templates);
        // Pre-select the first template whose expected sheets best match the workbook.
        const detected = this.detectBestTemplate(templates);
        if (detected) this.selectedApiId = detected;
      },
      error: () => {}
    });
  }

  convert(): void {
    this.status.set('converting');
    this.conversionResult.set(null);

    this.jsonConversionService.convertWorkbook({
      fileId: this.fileId,
      apiId:  this.selectedApiId
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

  private detectBestTemplate(templates: MappingTemplate[]): string | null {
    if (!templates.length || !this.presentSheetNames.length) return null;
    const lower = new Set(this.presentSheetNames.map(n => n.toLowerCase()));

    let best: { apiId: string; matches: number } | null = null;
    for (const tmpl of templates) {
      const matches = tmpl.sheetMappings.filter(m =>
        lower.has(m.sheetName.toLowerCase())
      ).length;
      if (!best || matches > best.matches) best = { apiId: tmpl.apiId, matches };
    }
    return best && best.matches > 0 ? best.apiId : null;
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
