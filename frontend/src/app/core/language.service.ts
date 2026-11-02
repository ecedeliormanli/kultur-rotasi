import { Injectable, inject, signal } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { TranslocoService } from '@jsverse/transloco';

export const languages = ['tr', 'en'] as const;
export type Language = (typeof languages)[number];

const storageKey = 'kultur-rotasi.language';

/**
 * Single source of truth for the current language. It switches the UI texts (Transloco),
 * the content language requested from the API (Accept-Language) and the <html lang> attribute.
 */
@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly transloco = inject(TranslocoService);
  private readonly document = inject(DOCUMENT);

  readonly current = signal<Language>(this.initialLanguage());

  constructor() {
    this.apply(this.current());
  }

  use(language: Language): void {
    this.current.set(language);
    this.apply(language);
    try {
      localStorage.setItem(storageKey, language);
    } catch {
      // Storage can be unavailable (private mode); the choice then lasts for this visit only
    }
  }

  private apply(language: Language): void {
    this.transloco.setActiveLang(language);
    this.document.documentElement.lang = language;
  }

  private initialLanguage(): Language {
    try {
      const saved = localStorage.getItem(storageKey);
      if (saved === 'tr' || saved === 'en') {
        return saved;
      }
    } catch {
      // Ignore and fall back to the browser language
    }
    return navigator.language.toLowerCase().startsWith('tr') ? 'tr' : 'en';
  }
}
