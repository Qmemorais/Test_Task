# Test_Task
Программа тестировалась для введенной ссылки, т.к. поиск всех страниц занимает много времени. Однако, данное приложение выполняет все необходимые функции:
- сканирование страницы и нахождение всех ссылок с типом Content/Type = "text/html"
- сканирование всех страниц, которые имеют то же начало ссылки, что и самая первая ссылка и нахождение всех ссылок с типом Content/Type = "text/html"
- попытка загрузки sitemap.xml и исследование ссылок на отсутствие дубликатов
- сравнение ссылок с sitemap.xml и лично отсканированных
- вывод ссылок, что есть в sitemap.xml но нет в отсканированном (1)
- вывод ссылок, что есть в отсканированном но нет в sitemap.xml (2)
- вывод времени отклика страницы для отсканированных ссылок
