import { Component, type ErrorInfo, type ReactNode } from "react";

interface ErrorBoundaryProps {
  children: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
}

/** Generic error state (section 58) — catches render-time errors anywhere in the tree instead of a blank white screen. */
class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { hasError: false };

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // Technical details stay in the console only — never shown to the user (section 60).
    console.error("Unhandled UI error:", error, info.componentStack);
  }

  private handleReload = () => window.location.reload();

  private handleGoBack = () => {
    this.setState({ hasError: false });
    window.history.back();
  };

  render() {
    if (!this.state.hasError) return this.props.children;

    return (
      <div className="container" style={{ textAlign: "center", padding: "var(--space-16) var(--space-6)" }}>
        <h1 style={{ textTransform: "uppercase", fontSize: "1.75rem" }}>Something went wrong</h1>
        <p style={{ maxWidth: 420, margin: "0 auto var(--space-8)" }}>
          Please try again. If the problem continues, contact support.
        </p>
        <div className="row" style={{ justifyContent: "center" }}>
          <button type="button" onClick={this.handleReload}>
            Try Again
          </button>
          <button type="button" className="btn-secondary" onClick={this.handleGoBack}>
            Go Back
          </button>
        </div>
      </div>
    );
  }
}

export default ErrorBoundary;
