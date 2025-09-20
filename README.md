# AI Calendar - Enterprise-Grade AI-Powered Calendar Management System

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4?style=flat-square)](https://docs.microsoft.com/aspnet/core)
[![Microsoft Graph](https://img.shields.io/badge/Microsoft%20Graph-API-0078D4?style=flat-square)](https://docs.microsoft.com/graph)
[![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4-00A67E?style=flat-square)](https://openai.com)
[![.NET Aspire](https://img.shields.io/badge/.NET%20Aspire-9.3-512BD4?style=flat-square)](https://docs.microsoft.com/dotnet/aspire)
[![Azure](https://img.shields.io/badge/Azure%20AD-Authentication-0078D4?style=flat-square)](https://azure.microsoft.com/services/active-directory)

## Project Overview

AI Calendar is a sophisticated, enterprise-grade calendar management system that seamlessly integrates artificial intelligence with Microsoft Graph API to provide intelligent calendar operations through natural language processing. The application demonstrates advanced software architecture patterns, modern cloud technologies, and AI-driven user experiences.

### Key Objectives

- **AI-Powered Calendar Management**: Natural language processing for calendar operations
- **Enterprise Security**: Azure AD integration with JWT authentication and OAuth 2.0
- **Microservices Architecture**: Modular, scalable design with clear separation of concerns
- **Modern Development Stack**: Cutting-edge .NET 8 features and cloud-native patterns
- **Real-time Communication**: Advanced AI chat interface with contextual understanding
- **Microsoft Graph Integration**: Full calendar synchronization with Office 365/Outlook

## Architecture Overview

The application follows a sophisticated **microservices architecture** with **Domain-Driven Design (DDD)** principles, implementing enterprise patterns such as **Repository Pattern**, **Dependency Injection**, and **Clean Architecture**.

```mermaid
graph TB
    %% User Interface Layer (Top)
    subgraph "🎨 Presentation Layer"
        direction LR
        A[📱 Razor Pages Client]
        B[💬 AI Chat Interface]
    end
    
    %% API Gateway Layer
    subgraph "🚪 API Gateway Layer"
        direction LR
        C[📅 Calendar API]
        D[🤖 LLM API]
    end
    
    %% AI Processing happens here in the flow
    subgraph "🧠 AI Processing Layer"
        direction LR
        E[ChatGPT-4 Integration]
        F[Model Context Protocol Server]
        G[Natural Language Processing]
    end
    
    %% Business Logic Layer
    subgraph "⚙️ Business Logic Layer"
        direction LR
        H[📅 Calendar Service]
        I[🔐 Authentication Service]
        J[👤 User Service]
    end
    
    %% Data Access Layer
    subgraph "💾 Data Layer"
        direction LR
        K[Entity Framework Core]
        L[🗄️ SQL Server]
        M[📊 Microsoft Graph API]
    end
    
    %% Infrastructure Layer (Bottom)
    subgraph "🏗️ Infrastructure Layer"
        direction LR
        N[.NET Aspire Orchestration]
        O[🔒 Azure AD Authentication]
        P[📈 OpenTelemetry Monitoring]
    end
    
    %% User Flow Connections
    A -->|HTTP Requests| C
    B -->|AI Queries| D
    
    %% API to AI Processing
    D -->|Process Queries| E
    E -->|Context & Protocol| F
    F -->|Language Analysis| G
    G -->|Structured Response| D
    
    %% API to Business Logic
    C -->|Calendar Operations| H
    D -->|Business Rules| H
    
    %% Business Logic to Data
    H -->|Data Access| K
    H -->|External Calendar Data| M
    I -->|User Authentication| O
    J -->|User Data| K
    
    %% Data Layer
    K -->|Database Operations| L
    
    %% Infrastructure Support
    N -.->|Orchestrates| C
    N -.->|Orchestrates| D
    O -.->|Provides Auth| I
    P -.->|Monitors| N
    
    %% Cross-cutting concerns
    I -.->|Auth Context| H
    I -.->|Auth Context| J
    
    %% Styling for better visual hierarchy
    classDef presentationLayer fill:#e1f5fe,stroke:#01579b,stroke-width:2px
    classDef apiLayer fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef aiLayer fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef businessLayer fill:#e8f5e8,stroke:#1b5e20,stroke-width:2px
    classDef dataLayer fill:#fce4ec,stroke:#880e4f,stroke-width:2px
    classDef infraLayer fill:#f1f8e9,stroke:#33691e,stroke-width:2px
    
    class A,B presentationLayer
    class C,D apiLayer
    class E,F,G aiLayer
    class H,I,J businessLayer
    class K,L,M dataLayer
    class N,O,P infraLayer
```

## AI Chat Interaction Flow

```mermaid
sequenceDiagram
    participant U as User
    participant UI as Chat UI
    participant LLM as LLM API
    participant MCP as MCP Server
    participant Graph as Microsoft Graph
    
    U->>UI: "Schedule meeting tomorrow 2pm"
    UI->>LLM: Process Natural Language
    LLM->>LLM: Intent & MCP Tool Recognition
    LLM->>LLM: Extract Tool Arguments
    LLM->>MCP: Call create_calendar_event(arguments)
    MCP->>Graph: Create Event via Graph API
    Graph->>MCP: Event Created Response
    MCP->>LLM: MCP Tool Execution Result
    LLM->>LLM: Draft User Response
    LLM->>UI: Formatted Natural Response
    UI->>U: "Meeting scheduled successfully for tomorrow at 2pm!"
```

## Technology Stack

### **Core Technologies**
- **.NET 8** - Latest LTS framework with enhanced performance and features
- **ASP.NET Core 8** - High-performance web framework
- **Entity Framework Core 9** - Advanced ORM with SQL Server integration
- **Razor Pages** - Server-side rendered UI with modern web patterns

### **AI & Machine Learning**
- **OpenAI GPT-4** - Advanced natural language processing via GitHub Models
- **Microsoft Extensions AI** - Unified AI abstractions for .NET
- **Model Context Protocol (MCP)** - Advanced AI tool orchestration
- **Semantic Kernel Integration** - AI workflow management

### **Authentication & Security**
- **Azure Active Directory** - Enterprise identity management
- **Microsoft Identity Web** - Seamless Azure AD integration
- **JWT Bearer Authentication** - Stateless token-based security
- **OAuth 2.0 / OpenID Connect** - Industry-standard authentication flows
- **On-Behalf-Of (OBO) Flow** - Secure token delegation for Microsoft Graph

### **Cloud & Infrastructure**
- **.NET Aspire** - Cloud-native application orchestration
- **Microsoft Graph API** - Office 365 calendar integration
- **OpenTelemetry** - Comprehensive observability and monitoring
- **SQL Server** - Enterprise-grade database with EF Core
- **Service Discovery** - Dynamic service resolution

### **Development Patterns**
- **Clean Architecture** - Layered, maintainable code structure
- **Repository Pattern** - Data access abstraction
- **Dependency Injection** - IoC container for loose coupling
- **CQRS Principles** - Command and query separation
- **Domain-Driven Design** - Business logic encapsulation
 

