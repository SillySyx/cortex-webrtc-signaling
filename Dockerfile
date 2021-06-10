FROM node:alpine
	WORKDIR /app

	ADD package.json /app
	ADD index.js /app

	RUN npm i

	EXPOSE 443

	ENTRYPOINT ["node", "index.js"]