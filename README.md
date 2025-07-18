# View AI Personal

<p align="center">
  <br />
  <br />
  <img src="assets/logo.png" alt="View AI Personal Logo" height="128">
  <br />
  <br />
</p>

<p align="center">
  <strong>Transform your personal documents into intelligent, searchable knowledge bases in minutes</strong>
  <br />
</p>

<p align="center">
  <a href="https://github.com/view-io/personal/releases">
    <img src="https://img.shields.io/github/v/release/view-io/personal" alt="Latest Release">
  </a>
  <a href="https://github.com/view-io/personal/blob/main/LICENSE">
    <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License">
  </a>
  <a href="https://github.com/view-io/personal/issues">
    <img src="https://img.shields.io/github/issues/view-io/personal" alt="Issues">
  </a>
</p>

View AI Personal is a cross-platform desktop application that empowers you to create intelligent, searchable knowledge bases from your personal documents. Simply drag and drop your files, choose your preferred AI provider (local or cloud), and start having intelligent conversations with your data.

> **Note**: This application is in its early stages. Please be patient and kind as we continue to improve and expand its capabilities.

## Getting Started

Please refer to [GETTING_STARTED.md](GETTING_STARTED.md) for how to get started quickly.

## ✨ New in Version v1.0.x

🎉 **Initial Release Features:**
- **Drag-and-Drop Simplicity**: Effortlessly ingest documents by dragging them into the application
- **Real-Time File Monitoring**: Automatically watch directories and subdirectories for changes
- **Multiple AI Providers**: Choose between local processing (Ollama), enterprise deployment (View AI), or cloud services (OpenAI/Claude)
- **Local Knowledge Bases**: Create and manage multiple knowledge bases stored securely on your machine
- **Intelligent Document Processing**: Semantic chunking and vector embeddings for search, retrieval, and chat
- **Cross-Platform Support**: Available for Windows, macOS, and Linux
- **Privacy-First Design**: Keep your data local when using Ollama or View AI deployments

## 🚀 Use Cases

### Personal Knowledge Management
Transform your personal document collection into an intelligent research assistant:
- **Research Papers & Articles**: Quickly find relevant information across PDFs and documents
- **Meeting Notes & Presentations**: Ask questions about past meetings, decisions, and action items
- **Financial Documents**: Get answers about expenses, contracts, and financial records
- **Learning Materials**: Create study guides from textbooks, course materials, and documentation

### Professional Document Analysis
Streamline your workflow with intelligent document processing:
- **Legal Document Review**: Quickly analyze contracts, agreements, and legal precedents
- **Technical Documentation**: Navigate complex manuals, specifications, and code documentation
- **Business Intelligence**: Extract insights from reports, spreadsheets, and business documents
- **Compliance & Audit**: Efficiently search through regulatory documents and compliance materials

### Content Creation & Writing
Enhance your writing process with AI-powered research:
- **Blog Post Research**: Gather information from multiple sources for comprehensive articles
- **Academic Writing**: Analyze research papers and generate citations from your document library
- **Creative Writing**: Use your personal notes and research as inspiration for stories and scripts

## 🔧 Installation

### Prerequisites
- **System Requirements**: Minimum 8GB RAM recommended
- **Storage**: Ensure adequate disk space for your knowledge bases (processed documents require additional storage)
- **For Local Processing**: Install [Ollama](https://ollama.ai/) for local embeddings and completions

### Installing Ollama (Recommended)
If you want to keep all processing local:

1. Visit [https://ollama.ai/](https://ollama.ai/)
2. Download and install for your operating system
3. Pull a model: `ollama pull llama3.1:8b` (or your preferred model)
4. Configure the model in View AI Personal settings

### Installing View AI Personal

1. Go to the [Releases](https://github.com/view-io/personal/releases) page
2. Download the installer for your operating system:
   - Windows: `ViewAI-Personal-Setup-v1.0.x.exe`
   - macOS: `ViewAI-Personal-v1.0.x.dmg`
   - Linux: `ViewAI-Personal-v1.0.x.AppImage`
3. Run the installer and follow the setup instructions

## 📚 Supported File Types

View AI Personal supports a wide range of document formats:
- **PDF Documents** (.pdf)
- **Microsoft Office** (.docx, .xlsx, .pptx)
- **Text Files** (.txt, .md, .rtf)
- **Legacy Office** (.doc, .xls, .ppt)

## 🛡️ Privacy & Data Control

Your privacy is our priority:
- **Local Processing**: When using Ollama or View AI deployments, all data remains on your infrastructure
- **Cloud Processing**: OpenAI and Claude integrations send only necessary context for processing
- **Data Sovereignty**: You maintain complete control over your documents and generated knowledge bases
- **No Data Collection**: View does not collect or store any of your personal information

## 🏢 Shameless Plug for View AI

Ready to scale beyond personal use? [**View AI**](https://view.io) offers enterprise-grade AI infrastructure software that transforms how organizations discover, process, and interact with their data.

### Why View AI Enterprise?
- **Complete Data Control**: Deploy on-premises or in your hybrid cloud environment
- **Enterprise Security**: HIPAA, GDPR, CCPA, and PCI compliant
- **Scalable Architecture**: Process terabytes of data across your organization
- **Advanced Features**: Multi-user collaboration, advanced analytics, and custom integrations
- **Professional Support**: Enterprise-grade support and professional services

### Key Enterprise Benefits:
- **Speed to Value**: Deploy production-ready AI in minutes, not months
- **Data Sovereignty**: Keep sensitive data within your security perimeter
- **Cost Predictability**: Transparent pricing at $0.30 per 1,000 words ingested
- **Flexible Deployment**: Support for any AI model, data source, and deployment method
- **Get Started Free**: $50 free credit for signing up

## 🤝 Contributing

We welcome contributions from the community! Whether you're fixing bugs, adding features, or improving documentation, your help makes View AI Personal better for everyone.

### How to Contribute
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Test thoroughly
5. Submit a pull request using our [PR template](.github/PULL_REQUEST_TEMPLATE.md)

### Development Guidelines
- **Language**: C# with .NET 9.0
- **UI Framework**: Avalonia UI for cross-platform compatibility
- **Code Style**: Follow standard C# conventions and best practices
- **Testing**: Include unit tests where possible for new features
- **Documentation**: Update documentation for any new features

## 🐛 Issues

Found a bug or have a feature request? We'd love to hear from you!

- **Bug Reports**: [Create an issue](https://github.com/view-io/personal/issues/new) with detailed steps to reproduce
- **Feature Requests**: Share your ideas for new features and improvements
- **Questions**: Use [Discussions](https://github.com/view-io/personal/discussions) for general questions and community support

---

<p align="center">
  <strong>Ready to get started?</strong><br>
  <a href="https://github.com/view-io/personal/releases">Download View AI Personal</a> | 
  <a href="https://view.io">Learn about View AI Enterprise</a> | 
  <a href="https://github.com/view-io/personal/discussions">Join the Community</a>
</p>

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [Avalonia UI](https://avaloniaui.net/) for cross-platform compatibility
- Powered by [LiteGraph](https://github.com/litegraph-js/litegraph.js) for knowledge base management
- Inspired by the open-source community and our amazing users

---

*View AI Personal - Transform your documents into intelligent conversations.*