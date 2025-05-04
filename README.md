# 🖥️ System Monitoring Tool

A lightweight, cross-platform system monitoring tool built using **.NET 8**, featuring:

- Real-time CPU, RAM, and Disk monitoring
- Plugin-based architecture for flexible logging and alerting
- REST API to expose collected metrics
- Slack alerts for high CPU usage
- Auto-refreshing browser UI for live visualization

---

## 📦 Features

- ⏱️ **Periodic Monitoring**: Configurable interval to collect system resource usage
- 🔌 **Plugin System**: Easily add plugins (e.g., API poster, file logger)
- 🌐 **Minimal Web API**: `/api/metrics` for POST and GET endpoints
- 📊 **Live Dashboard**: HTML page that polls API every 5 seconds
- 🚨 **Slack Alerts**: Sends notifications when CPU usage exceeds 80%
- 🪟 **Cross-platform**: Works on Windows, Linux, and macOS

---

## 🛠️ Technologies Used

- .NET 8
- ASP.NET Core Minimal API
- JSON-based configuration (`appsettings.json`)
- Plugin architecture using interfaces
- HTML + JS (no frontend framework)

---

## ⚙️ Configuration

Update your `appsettings.json`:

```json
{
  "ApiEndpoint": "http://localhost:5000",
  "MonitoringIntervalSeconds": 5,
  "SlackWebhookUrl": "https://hooks.slack.com/services/your/webhook/url"
}



