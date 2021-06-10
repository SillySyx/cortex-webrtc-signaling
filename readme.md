# Deploying to Azure Registry

```
docker login silly.azurecr.io

docker build -t silly.azurecr.io/cortex-webrtc:0.1 .
docker push silly.azurecr.io/cortex-webrtc:0.1
```



# Deploying to Heroku Registry

```
heroku login

heroku container:login

heroku container:push --app cortex-webrtc-signaling web
heroku container:release --app cortex-webrtc-signaling web
```





# Deploying to Heroku Git

```
heroku login
heroku git:remote -a cortex-webrtc-signaling
```

```
git push heroku master
```