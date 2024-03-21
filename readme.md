Репозиторий для общих утилилитарных пакетов. 

### Опубликовать пакет 
Подкручиваем версию в файле проекта, собираем **nuget** пакет : 
```bash
dotnet pack -c Release .\src\InDuckTor.Shared\InDuckTor.Shared.csproj
```
Публикуем пакет в **github** :
```bash
dotnet nuget push .\src\InDuckTor.Shared\bin\Release\InDuckTor.Shared.<Версия>.nupkg -s https://nuget.pkg.github.com/in-duck-tor/index.json -k <GitHub personal access token>
```