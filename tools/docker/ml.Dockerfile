FROM python:3.12-slim
WORKDIR /repo/ml
COPY ml/requirements.txt /tmp/requirements.txt
RUN pip install --no-cache-dir -r /tmp/requirements.txt
