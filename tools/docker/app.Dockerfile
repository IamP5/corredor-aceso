FROM node:22-slim
WORKDIR /app
COPY app/package.json app/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY app/ ./
ENV CI=1
EXPOSE 8081
CMD ["npx", "expo", "start", "--web", "--port", "8081"]
