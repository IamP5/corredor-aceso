FROM python:3.12-slim
WORKDIR /repo
COPY iot/simulador/requirements.txt /tmp/requirements.txt
RUN pip install --no-cache-dir -r /tmp/requirements.txt
COPY iot/simulador/ iot/simulador/
ENTRYPOINT ["python", "iot/simulador/simulador.py"]
