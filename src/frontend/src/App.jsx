import React, { useState, useEffect } from 'react';
import { CheckCircle2Icon, ListTodoIcon, PlusIcon, Trash2Icon } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { ModeToggle } from '@/components/mode-toggle';
import { cn } from '@/lib/utils';

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
      if (!res.ok) throw new Error(`Failed to create todo (${res.status} ${res.statusText})`);
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

  const remaining = todos.filter(todo => !todo.isCompleted).length;

  return (
    <div className="app-aurora min-h-svh px-4 py-10 sm:py-16">
      <div className="mx-auto w-full max-w-2xl">
        <header className="mb-8 flex items-start justify-between gap-4">
          <div className="space-y-1.5">
            <h1 className="flex items-center gap-2.5 text-2xl font-semibold tracking-tight sm:text-3xl">
              <span
                aria-hidden="true"
                className="flex size-10 items-center justify-center rounded-xl bg-primary/10 text-primary ring-1 ring-primary/20"
              >
                <ListTodoIcon className="size-5" />
              </span>
              📋 Todo App
            </h1>
            {user && (
              <p className="text-sm text-muted-foreground">
                Signed in as <strong className="font-medium text-foreground">{user.userDetails}</strong>
              </p>
            )}
          </div>

          <div className="flex shrink-0 items-center gap-1">
            <ModeToggle />
            {user && (
              <Button variant="ghost" size="sm" asChild>
                <a href="/logout">Sign out</a>
              </Button>
            )}
          </div>
        </header>

        {error && (
          <div
            role="alert"
            className="mb-6 rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm font-medium text-destructive"
          >
            {error}
          </div>
        )}

        <Card className="mb-6 shadow-sm">
          <CardHeader>
            <CardTitle className="text-base">Add a task</CardTitle>
            <CardDescription>Keep it short — you can add detail underneath.</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={addTodo} className="space-y-3">
              <Input
                type="text" placeholder="Todo title..." value={title}
                onChange={e => setTitle(e.target.value)}
                aria-label="Todo title"
              />
              <Input
                type="text" placeholder="Description (optional)" value={description}
                onChange={e => setDescription(e.target.value)}
                aria-label="Todo description"
              />
              <Button type="submit" className="w-full sm:w-auto">
                <PlusIcon />
                Add Todo
              </Button>
            </form>
          </CardContent>
        </Card>

        <div className="mb-3 flex items-center justify-between px-1">
          <h2 className="text-sm font-medium text-muted-foreground">
            {todos.length === 0 ? 'No tasks yet' : `${remaining} of ${todos.length} remaining`}
          </h2>
        </div>

        {todos.length === 0 ? (
          <div className="rounded-xl border border-dashed px-6 py-14 text-center">
            <CheckCircle2Icon className="mx-auto mb-3 size-8 text-muted-foreground/50" />
            <p className="text-sm text-muted-foreground">
              Nothing here yet. Add your first task above.
            </p>
          </div>
        ) : (
          <ul className="space-y-2">
            {todos.map(todo => (
              <li
                key={todo.id}
                className="group flex items-start gap-3 rounded-xl border bg-card p-4 shadow-xs transition-colors hover:border-primary/30 hover:bg-accent/40"
              >
                <Checkbox
                  checked={todo.isCompleted}
                  onCheckedChange={() => toggleTodo(todo.id)}
                  aria-label={`Toggle ${todo.title}`}
                  className="mt-0.5"
                />
                <span className="min-w-0 flex-1">
                  <strong
                    className={cn(
                      'block font-medium break-words transition-colors',
                      todo.isCompleted && 'text-muted-foreground line-through'
                    )}
                  >
                    {todo.title}
                  </strong>
                  {todo.description && (
                    <small className="mt-0.5 block text-sm break-words text-muted-foreground">
                      {todo.description}
                    </small>
                  )}
                </span>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => deleteTodo(todo.id)}
                  aria-label={`Delete ${todo.title}`}
                  className="shrink-0 text-muted-foreground opacity-0 transition-opacity hover:text-destructive focus-visible:opacity-100 group-hover:opacity-100"
                >
                  <Trash2Icon />
                </Button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

export default App;
