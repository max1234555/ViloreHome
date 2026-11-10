# Git workflow: fork → pull request → review

У GitHub репозиторії для читання може бути недостатньо прав на push. Якщо push до `max1234555/ViloreHome` завершується `403`, потрібно надсилати зміни через власний fork або попросити власника надати роль Write.

## Варіант через fork

1. На GitHub відкрий `max1234555/ViloreHome` та натисни **Fork**.
2. Клонуй власний fork або зміни віддалені адреси в наявній копії. Для наявного клону:

   ```powershell
   git remote rename origin upstream
   git remote add origin https://github.com/<твій-логін>/ViloreHome.git
   git remote -v
   ```

3. Створи окрему гілку від актуального `main`:

   ```powershell
   git fetch upstream
   git switch main
   git pull upstream main
   git switch -c lab3-ddd
   ```

4. Додай потрібні файли, створи коміт і відправ гілку у свій fork:

   ```powershell
   git add docs/domain Web-prog_server/Domain
   git commit -m "Lab3: add DDD domain model"
   git push -u origin lab3-ddd
   ```

5. На GitHub створи Pull Request: base — `max1234555/ViloreHome` / `main`, compare — твій fork / `lab3-ddd`. В описі вкажи, що додано, і як перевіряв зміни.
6. Власник або інший учасник переглядає PR, залишає коментарі чи просить зміни. Внеси виправлення в ту саму гілку й запуш їх; PR оновиться автоматично.
7. Після схвалення PR зливають у `main`. Не зливай PR самостійно, якщо це має зробити викладач або власник репозиторію.

Якщо власник уже надав тобі роль Write, можна працювати у гілці власного клону без fork; решта процесу з PR та review залишається такою самою.
