import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import App from '../src/App.jsx';

function jsonResponse(data) {
  return Promise.resolve({ ok: true, json: () => Promise.resolve(data) });
}

describe('Todo App', () => {
  beforeEach(() => {
    global.fetch = vi.fn(url => url === '/.auth/me'
      ? jsonResponse({ clientPrincipal: null })
      : jsonResponse([]));
  });

  it('renders the app title', async () => {
    render(<App />);
    expect(screen.getByText('📋 Todo App')).toBeInTheDocument();
  });

  it('shows error when title is empty', async () => {
    render(<App />);
    fireEvent.click(screen.getByText('Add Todo'));
    expect(screen.getByRole('alert')).toHaveTextContent('Title is required');
  });

  it('displays todos from API', async () => {
    const mockTodos = [
      { id: 1, title: 'Test Todo', description: 'A test', isCompleted: false }
    ];
    global.fetch.mockImplementation(url => url === '/.auth/me'
      ? jsonResponse({ clientPrincipal: null })
      : jsonResponse(mockTodos));
    render(<App />);
    await waitFor(() => expect(screen.getByText('Test Todo')).toBeInTheDocument());
  });

  it('displays the signed-in user and logout link', async () => {
    global.fetch.mockImplementation(url => url === '/.auth/me'
      ? jsonResponse({ clientPrincipal: { userDetails: 'alex@example.com' } })
      : jsonResponse([]));

    render(<App />);

    await waitFor(() => expect(screen.getByText('alex@example.com')).toBeInTheDocument());
    expect(screen.getByRole('link', { name: 'Sign out' })).toHaveAttribute('href', '/logout');
  });

  it('calls toggle endpoint when checkbox clicked', async () => {
    const mockTodos = [
      { id: 1, title: 'Toggle Me', description: '', isCompleted: false }
    ];
    let todoFetchCount = 0;
    global.fetch.mockImplementation((url, options) => {
      if (url === '/.auth/me') return jsonResponse({ clientPrincipal: null });
      if (url === '/api/todo/1/toggle' && options?.method === 'PATCH') {
        return Promise.resolve({ ok: true });
      }
      if (url === '/api/todo') {
        todoFetchCount += 1;
        return jsonResponse(todoFetchCount === 1 ? mockTodos : []);
      }
      return jsonResponse([]);
    });

    render(<App />);
    await waitFor(() => expect(screen.getByText('Toggle Me')).toBeInTheDocument());
    fireEvent.click(screen.getByLabelText('Toggle Toggle Me'));
    expect(global.fetch).toHaveBeenCalledWith('/api/todo/1/toggle', { method: 'PATCH' });
  });
});
