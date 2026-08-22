export const environment = {
  production: true,
  // Set this to the deployed API origin plus /api before a production build.
  // No production API host is assumed by the repository.
  apiBaseUrl: '__CONFIGURE_PRODUCTION_API_BASE_URL__',
} as const;
