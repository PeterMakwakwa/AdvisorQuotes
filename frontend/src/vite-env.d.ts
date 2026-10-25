/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_TRACE_VIEWER_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
