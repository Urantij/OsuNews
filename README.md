# осу новости

![image](https://github.com/user-attachments/assets/9874f843-93ad-4266-afea-0b2c645b1e2a)


Опрашивает осу на предмет нового дейлика, а затем постит информацию о новом дейлике в дискорде.  
Ещё опрашивает ютуб на предмет нового видево на ютубе.

## запуск

https://osu.ppy.sh/home/account/edit тут нужно создать своё oauth приложение. Нужен скоуп "Read public data on your behalf."
Нужно от своего юзера сделать вход и получить рефреш токен.

В настройках канала дискорда раздел интеграции, создать вебхук.

Где то в гугл девелопер консоли нужно сделать приложение, подключить ютуб дата апи, а затем сделать где то там апи ключ.

## конфигурация

Конфиги, которые нужно указать приложению. Например, через appsettings.json  
Osu/OsuConfig.cs  
Newscasters/Discorb/DiscorderConfig.cs  
MyTube/TubeConfig.cs

Если не указать осу конфиг, не будет проверки на новый дейлик.  
Без конфига дискорда не будет писать в дискорд.  
Без конфига ютуба не будет проверять ютуб.