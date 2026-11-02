import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { Language, LanguageService, languages } from './core/language.service';

@Component({
  imports: [RouterOutlet, TranslocoPipe],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly language = inject(LanguageService);
  protected readonly languages = languages;

  protected use(language: Language): void {
    this.language.use(language);
  }
}
