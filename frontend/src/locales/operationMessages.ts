import { registerTranslations } from '@/i18n'

registerTranslations({
  en: {
    'operations.retry': 'Try again',
    'operations.refreshFailed': 'Saved successfully, but the data could not be refreshed. Try refreshing; do not submit again.',
    'operations.analyticsFailed': 'Unable to load expense categories.',
    'operations.analyticsLoading': 'Loading expense categories…',
    'operations.categorySaved': 'Category saved.',
    'operations.transferDebit': 'Transfer to another account',
    'operations.transferCredit': 'Transfer from another account',
    'operations.openingBalance': 'Opening balance',
  },
  pl: {
    'operations.retry': 'Spróbuj ponownie',
    'operations.refreshFailed': 'Zapisano pomyślnie, ale nie udało się odświeżyć danych. Spróbuj odświeżyć stronę; nie zapisuj ponownie.',
    'operations.analyticsFailed': 'Nie udało się pobrać kategorii wydatków.',
    'operations.analyticsLoading': 'Ładowanie kategorii wydatków…',
    'operations.categorySaved': 'Zapisano kategorię.',
    'operations.transferDebit': 'Przelew na inne konto',
    'operations.transferCredit': 'Przelew z innego konta',
    'operations.openingBalance': 'Saldo otwarcia',
  },
})
