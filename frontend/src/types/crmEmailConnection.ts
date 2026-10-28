export interface EmailConnectionStatusDto {
  isConfigured: boolean;
  isConnected: boolean;
  provider: string | null;
  emailAddress: string | null;
  status: string | null;
  lastSyncedAt: string | null;
  lastSyncError: string | null;
}

export interface AuthorizationUrlDto {
  authorizationUrl: string;
}
