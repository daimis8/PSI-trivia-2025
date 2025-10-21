import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

export function createGameHub(): HubConnection {
  const connection = new HubConnectionBuilder()
    .withUrl("/hubs/game", {
      withCredentials: true,
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build();
  return connection;
}
