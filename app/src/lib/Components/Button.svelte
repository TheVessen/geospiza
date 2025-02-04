<script lang="ts">
  import type { Snippet } from "svelte";

  type ButtonVariant = "primary" | "secondary" | "danger" | "ghost";
  type ButtonSize = "sm" | "md" | "lg";

  type ButtonProps = {
    variant?: ButtonVariant;
    size?: ButtonSize;
    disabled?: boolean;
    type?: "button" | "submit" | "reset";
    onclick?: () => void;
    children?: Snippet;
  };

  let {
    variant = "primary",
    size = "md",
    disabled,
    type,
    onclick,
    children,
  }: ButtonProps = $props();

  const sizeClasses = {
    sm: "px-3 py-1.5 text-sm",
    md: "px-4 py-2 text-base",
    lg: "px-6 py-3 text-lg",
  };

  const variantClasses = {
    primary: "bg-blue-500 hover:bg-blue-600 text-white",
    secondary: "bg-gray-500 hover:bg-gray-600 text-white",
    danger: "bg-red-500 hover:bg-red-600 text-white",
    ghost: "bg-transparent hover:bg-gray-100 text-gray-700",
  };
</script>

<button
  {type}
  class="inline-flex items-center justify-center rounded-lg font-medium
  transition-all duration-200 transform hover:scale-[1.02] active:scale-[0.98]
  disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100
  focus:outline-none focus:ring-2 focus:ring-offset-2
  {sizeClasses[size]}
  {variantClasses[variant]}"
  {disabled}
  {onclick}
>
  {@render children?.()}
</button>
