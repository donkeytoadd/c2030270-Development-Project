export type MappingStrategy = 'Specification' | 'Inventory' | 'Order' | 'FieldMapped' | 'Raw';

export interface TmForumApiDefinition {
  apiId: string;
  name: string;
  resourceType: string;
  basePath: string;
  resourceCollectionPath: string;
  version: string;
  mappingStrategy: MappingStrategy;
  characteristicArrayKey: string;
  orderItemNestedObjectKey: string;
}

export interface JsonConversionConfig {
  apiId: string;
  sheetNames?: string;
  nameColumn?: string;
  versionColumn?: string;
  descriptionColumn?: string;
  groupByColumn?: string;
  fieldMappings?: Record<string, string>;
}

export interface JsonConversionRequest {
  fileId: string;
  config?: JsonConversionConfig;
}

export type ValidationSeverity = 'Error' | 'Warning' | 'SchemaArtefact';

export interface ValidationIssue {
  resourceIndex: number;
  resourceName: string;
  field: string;
  message: string;
  severity: ValidationSeverity;
}

export interface ValidationReport {
  isValid: boolean;
  totalIssues: number;
  artefactCount: number;
  compliancePercentage: number;
  issues: ValidationIssue[];
}

export type TmForumResource = Record<string, unknown>;

export interface JsonConversionResponse {
  fileId: string;
  sheetName: string;
  apiId: string;
  apiName: string;
  resourceType: string;
  totalResources: number;
  validation: ValidationReport;
  resources: TmForumResource[];
  warnings: string[];
}

export interface ApiDetectionCandidate {
  apiId: string;
  apiName: string;
  confidence: number;
  matchedSignals: string[];
  missingSignals: string[];
}

export interface ApiDetectionResult {
  candidates: ApiDetectionCandidate[];
  autoSelectedApiId: string | null;
}

export interface ColumnMapping {
  sourceColumn: string;
  targetField: string;
}

export interface SubArrayDefinition {
  subArrayKey: string;
  subArrayColumns: string[];
}

export type SheetMappingPattern = 'TopLevelFields' | 'FlatArray' | 'NestedArray';

export interface SheetMapping {
  sheetName: string;
  pattern: SheetMappingPattern;
  targetKey: string;
  isRequired: boolean;
  columnMappings: ColumnMapping[];
  groupByColumn?: string;
  subArrayDefinitions: SubArrayDefinition[];
  parentIdColumn?: string;
  childParentRefColumn?: string;
  childArrayKey?: string;
}

export interface MappingTemplate {
  apiId: string;
  apiName: string;
  rootResourceType: string;
  rootBaseType?: string;
  basePath: string;
  resourceCollectionPath: string;
  sheetMappings: SheetMapping[];
}

export interface WorkbookConversionRequest {
  fileId: string;
  apiId?: string;
  template?: MappingTemplate;
}

export interface WorkbookConversionResponse {
  fileId: string;
  resultId: string;
  workbookName: string;
  apiId: string;
  apiName: string;
  resourceType: string;
  output: Record<string, unknown>;
  validation: ValidationReport;
  warnings: string[];
}
