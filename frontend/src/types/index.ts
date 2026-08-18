export interface User {
  id: string;
  userName: string;
  email: string;
  fullName: string;
  isActive: boolean;
  roles: string[];
  lastLoginAt: string | null;
  passwordExpired: boolean;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  userName: string;
  fullName: string;
  roles: string[];
  passwordExpired: boolean;
}

export interface BankAccount {
  id: string;
  iban: string;
  bankName: string;
  displayName: string;
  currency: string;
  isActive: boolean;
  consentValidUntil: string | null;
  latestBalance: number | null;
  latestBalanceDate: string | null;
  transactionCount: number;
}

export interface Balance {
  id: number;
  balanceType: string;
  amount: number;
  currency: string;
  referenceDate: string;
  fetchedAt: string;
}

export interface Transaction {
  id: number;
  amount: number;
  currency: string;
  creditDebitIndicator: 'DBIT' | 'CRDT';
  bookingDate: string;
  valueDate: string | null;
  description: string;
  counterpartyName: string | null;
  status: string;
}

export interface SyncLog {
  id: number;
  bankAccountId: string;
  syncTrigger: string;
  status: 'Success' | 'Failure';
  errorMessage: string | null;
  balancesFetched: number;
  transactionsFetched: number;
  startedAt: string;
  finishedAt: string | null;
}

export interface SyncStatus {
  todayCount: number;
  maxDaily: number;
  remaining: number;
}

export interface AuditLogEntry {
  id: number;
  userId: string | null;
  userName: string | null;
  action: string;
  httpMethod: string;
  path: string;
  statusCode: number;
  ipAddress: string | null;
  details: string | null;
  timestamp: string;
}

export interface ErrorLogEntry {
  id: number;
  source: string;
  message: string;
  stackTrace: string | null;
  path: string | null;
  userId: string | null;
  resolved: boolean;
  timestamp: string;
}
