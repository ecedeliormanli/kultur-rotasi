import { TranslocoTestingModule, TranslocoTestingOptions } from '@jsverse/transloco';
import en from '../../public/i18n/en.json';
import tr from '../../public/i18n/tr.json';

/** Real translation files, loaded synchronously, so tests can assert on visible texts. */
export function translocoTesting(options: TranslocoTestingOptions = {}) {
  return TranslocoTestingModule.forRoot({
    langs: { tr, en },
    translocoConfig: { availableLangs: ['tr', 'en'], defaultLang: 'tr' },
    preloadLangs: true,
    ...options,
  });
}
