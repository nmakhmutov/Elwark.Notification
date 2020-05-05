FROM openjdk:8-jre-alpine
EXPOSE 80

RUN mkdir /app

COPY ./build/libs/Elwark.Notification-all.jar /app/Elwark.Notification-all.jar
WORKDIR /app

CMD ["java", "-server", "-XX:+UnlockExperimentalVMOptions", "-XX:+UseCGroupMemoryLimitForHeap", "-XX:InitialRAMFraction=2", "-XX:MinRAMFraction=2", "-XX:MaxRAMFraction=2", "-XX:+UseG1GC", "-XX:MaxGCPauseMillis=100", "-XX:+UseStringDeduplication", "-jar", "Elwark.Notification-all.jar"]