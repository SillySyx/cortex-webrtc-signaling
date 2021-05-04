FROM node:alpine
	WORKDIR /app

	ADD src/package.json /app
	ADD src/index.js /app

	RUN npm i

	EXPOSE 8080

	ENTRYPOINT ["node", "index.js"]