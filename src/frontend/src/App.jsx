import React, { useState, useEffect } from 'react';

const API_URL = '/api/todo';

function App() {
  const [todos, setTodos] = useState([]);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [error, setError] = useState('');
  const [user, setUser] = useState(null);

  useEffect(() => { fetchTodos(); }, []);

  useEffect(() => {
    fetch('/.auth/me')
      .then(res => res.json())
      .then(data => setUser(data.clientPrincipal))
      .catch(() => setUser(null));
  }, []);

  async function fetchTodos() {
    try {
      const res = await fetch(API_URL);
      if (!res.ok) {
        throw new Error(`Failed to fetch todos (${res.status} ${res.statusText})`);
      }
      const data = await res.json();
      setTodos(data);
      setError('');
    } catch (err) {
      setError(err.message);
    }
  }

  async function addTodo(e) {
    e.preventDefault();
    if (!title.trim()) { setError('Title is required'); return; }
    try {
      const res = await fetch(API_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title, description })
      });
      if (!res.ok) throw new Error('Failed to create todo');
      setTitle(''); setDescription(''); setError('');
      fetchTodos();
    } catch (err) {
      setError(err.message);
    }
  }

  async function toggleTodo(id) {
    await fetch(`${API_URL}/${id}/toggle`, { method: 'PATCH' });
    fetchTodos();
  }

  async function deleteTodo(id) {
    await fetch(`${API_URL}/${id}`, { method: 'DELETE' });
    fetchTodos();
  }

  return (
    <div style={{ maxWidth: '600px', margin: '2rem auto', fontFamily: 'system-ui' }}>
      <h1>📋 Todo App</h1>
      {user && (
        <p style={{ display: 'flex', justifyContent: 'space-between' }}>
          <span>Signed in as <strong>{user.userDetails}</strong></span>
          <a href="/logout">Sign out</a>
        </p>
      )}
      {error && <p style={{ color: 'red' }} role="alert">{error}</p>}
      <form onSubmit={addTodo} style={{ marginBottom: '2rem' }}>
        <input
          type="text" placeholder="Todo title..." value={title}
          onChange={e => setTitle(e.target.value)}
          aria-label="Todo title"
          style={{ width: '100%', padding: '0.5rem', marginBottom: '0.5rem' }}
        />
        <input
          type="text" placeholder="Description (optional)" value={description}
          onChange={e => setDescription(e.target.value)}
          aria-label="Todo description"
          style={{ width: '100%', padding: '0.5rem', marginBottom: '0.5rem' }}
        />
        <button type="submit" style={{ padding: '0.5rem 1rem' }}>Add Todo</button>
      </form>
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {todos.map(todo => (
          <li key={todo.id} style={{ padding: '0.75rem', borderBottom: '1px solid #eee', display: 'flex', alignItems: 'center', gap: '1rem' }}>
            <input type="checkbox" checked={todo.isCompleted} onChange={() => toggleTodo(todo.id)} aria-label={`Toggle ${todo.title}`} />
            <span style={{ flex: 1, textDecoration: todo.isCompleted ? 'line-through' : 'none' }}>
              <strong>{todo.title}</strong>
              {todo.description && <small style={{ display: 'block', color: '#666' }}>{todo.description}</small>}
            </span>
            <button onClick={() => deleteTodo(todo.id)} aria-label={`Delete ${todo.title}`}>🗑️</button>
          </li>
        ))}
      </ul>
    </div>
  );
}

export default App;
