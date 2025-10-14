// Email validation
export function validateEmail(email: string): string {
  if (!email) {
    return "Email is required";
  }
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!emailRegex.test(email)) {
    return "Please enter a valid email address";
  }
  return "";
}

// Password validation
export function validatePassword(password: string): string {
  if (!password) {
    return "Password is required";
  }
  if (password.length < 8) {
    return "Password must be at least 8 characters long";
  }
  return "";
}

export function validateLoginPassword(password: string): string {
  if (!password) {
    return "Password is required";
  }
  return "";
}

// Confirm password validation
export function validateConfirmPassword(
  password: string,
  confirmPassword: string
): string {
  if (!confirmPassword) {
    return "Please confirm your password";
  }
  if (password !== confirmPassword) {
    return "Passwords do not match";
  }
  return "";
}

// Username validation
export function validateUsername(username: string): string {
  if (!username) {
    return "Username is required";
  }
  if (username.length < 3) {
    return "Username must be at least 3 characters long";
  }
  if (username.length > 15) {
    return "Username must not exceed 15 characters";
  }
  return "";
}

