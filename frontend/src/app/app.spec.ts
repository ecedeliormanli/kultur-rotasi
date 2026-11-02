import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { translocoTesting } from '../testing/transloco-testing';

describe('App', () => {
  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [App, translocoTesting()],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('shows the product name', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const header = fixture.nativeElement as HTMLElement;
    expect(header.querySelector('h1')?.textContent).toContain('Kültür Rotası');
  });

  it('switches the language and remembers it', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    const english = [...element.querySelectorAll('button')].find(
      (b) => b.textContent?.trim() === 'EN',
    )!;
    english.click();
    await fixture.whenStable();

    expect(element.querySelector('header p')?.textContent).toContain('Map of museums');
    expect(document.documentElement.lang).toBe('en');
    expect(localStorage.getItem('kultur-rotasi.language')).toBe('en');
  });
});
