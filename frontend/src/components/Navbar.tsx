import { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "@tanstack/react-router";
import { useAuth } from "@/context/AuthContext";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuItem,
	DropdownMenuLabel,
	DropdownMenuSeparator,
	DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { searchProfiles, type UserSearchResult } from "@/lib/profile";
import { getCreatorBadge, getQuizzerBadge } from "@/lib/badges";
import { User, LogOut, Home, CircleQuestionMark, Play, Search, Loader2 } from "lucide-react";

export function Navbar() {
	const { isAuthenticated, user, logout } = useAuth();
	const navigate = useNavigate();

	const handleLogout = async () => {
		await logout();
		navigate({ to: "/login" });
	};

	const handleViewProfile = () => {
		navigate({ to: "/profile" });
	};

	const handleSelectProfile = (profileId: number) => {
		navigate({ to: `/profile/${profileId}` });
	};

	return (
		<nav className="border-b bg-white shadow-sm">
			<div className="container mx-auto px-4">
				<div className="flex h-16 items-center justify-between gap-4">
					{/* Left side - Logo and Home */}
					<div className="flex items-center gap-6">
						<Link
							to="/"
							className="flex items-center gap-2 hover:opacity-80 transition-opacity"
						>
							<span className="text-2xl font-bold text-primary">
								Trivia
							</span>
						</Link>
						{isAuthenticated && (
							<>
								<Link to="/">
									<Button
										variant="ghost"
										size="sm"
										className="gap-2"
									>
										<Home className="h-4 w-4" />
										Home
									</Button>
								</Link>
								<Link to="/my-quizzes">
									<Button
										variant="ghost"
										size="sm"
										className="gap-2"
									>
										<CircleQuestionMark className="h-4 w-4" />
										My Quizzes
									</Button>
								</Link>
							</>
						)}
					</div>

					{/* Right side - User menu or Auth links */}
					<div className="flex flex-1 items-center justify-end gap-4 overflow-visible">
						<Link to="/play">
							<Button className="whitespace-nowrap">
								<Play />
								Play
							</Button>
						</Link>


						{!isAuthenticated ? (
							<>
								<Link to="/login">
									<Button variant="ghost">Login</Button>
								</Link>
								<Link to="/register">
									<Button variant="outline">Register</Button>
								</Link>
							</>
						) : (
							<>
								<ProfileSearch onSelectProfile={handleSelectProfile} />
								<DropdownMenu>
									<DropdownMenuTrigger asChild>
										<Button
											variant="ghost"
											className="flex items-center gap-2 h-auto py-2"
										>
											<Avatar className="h-8 w-8">
												<AvatarFallback className="bg-primary text-primary-foreground">
													{user?.username ? getInitials(user.username) : "U"}
												</AvatarFallback>
											</Avatar>
											<span className="text-sm font-medium">
												{user?.username}
											</span>
										</Button>
									</DropdownMenuTrigger>
									<DropdownMenuContent align="end" className="w-56">
										<DropdownMenuLabel>My Account</DropdownMenuLabel>
										<DropdownMenuSeparator />
										<DropdownMenuItem
											onClick={handleViewProfile}
											className="cursor-pointer"
										>
											<User className="mr-2 h-4 w-4" />
											<span>View Profile</span>
										</DropdownMenuItem>
										<DropdownMenuSeparator />
										<DropdownMenuItem
											onClick={handleLogout}
											className="cursor-pointer text-red-600 focus:text-red-600"
										>
											<LogOut className="mr-2 h-4 w-4" />
											<span>Logout</span>
										</DropdownMenuItem>
									</DropdownMenuContent>
								</DropdownMenu>
							</>
						)}
					</div>
				</div>
			</div>
		</nav>
	);
}

interface ProfileSearchProps {
	onSelectProfile: (userId: number) => void;
}

function ProfileSearch({ onSelectProfile }: ProfileSearchProps) {
	const [query, setQuery] = useState("");
	const [debouncedQuery, setDebouncedQuery] = useState("");
	const [results, setResults] = useState<UserSearchResult[]>([]);
	const [isFocused, setIsFocused] = useState(false);
	const [isSearching, setIsSearching] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const controllerRef = useRef<AbortController | null>(null);
	const blurTimeout = useRef<number>(0);

	useEffect(() => {
		const timeout = window.setTimeout(() => {
			setDebouncedQuery(query.trim());
		}, 300);
		return () => window.clearTimeout(timeout);
	}, [query]);

	useEffect(() => {
		controllerRef.current?.abort();

		if (debouncedQuery.length < 2) {
			setResults([]);
			setError(null);
			setIsSearching(false);
			return;
		}

		const controller = new AbortController();
		controllerRef.current = controller;
		setIsSearching(true);

		searchProfiles(debouncedQuery, 5, controller.signal)
			.then((data) => {
				setResults(data);
				setError(null);
			})
			.catch((err) => {
				if (err.name === "AbortError") {
					return;
				}
				setError("Search failed. Please try again.");
			})
			.finally(() => {
				if (!controller.signal.aborted) {
					setIsSearching(false);
				}
			});

		return () => controller.abort();
	}, [debouncedQuery]);

	const handleSelect = (id: number) => {
		onSelectProfile(id);
		setQuery("");
		setResults([]);
		setIsFocused(false);
	};

	const handleFocus = () => {
		if (blurTimeout.current) {
			window.clearTimeout(blurTimeout.current);
		}
		setIsFocused(true);
	};

	const handleBlur = () => {
		blurTimeout.current = window.setTimeout(() => {
			setIsFocused(false);
		}, 150);
	};

	const showDropdown =
		isFocused && (debouncedQuery.length >= 2 || isSearching || !!error);

	return (
		<div className="relative w-56 max-w-[14rem] min-w-0 shrink">
			<div className="relative">
				<Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
				<Input
					value={query}
					onChange={(event) => setQuery(event.target.value)}
					placeholder="Search profiles"
					onFocus={handleFocus}
					onBlur={handleBlur}
					className="pl-9"
				/>
			</div>
			{showDropdown && (
				<div className="absolute right-0 z-50 mt-2 w-full rounded-md border bg-white shadow-lg">
					<div className="max-h-72 overflow-y-auto py-2">
						{isSearching && (
							<div className="flex items-center gap-2 px-4 py-2 text-sm text-muted-foreground">
								<Loader2 className="size-4 animate-spin" />
								Searching...
							</div>
						)}
						{!isSearching && error && (
							<div className="px-4 py-2 text-sm text-destructive">
								{error}
							</div>
						)}
						{!isSearching && !error && results.length === 0 && debouncedQuery.length >= 2 && (
							<div className="px-4 py-2 text-sm text-muted-foreground">
								No profiles found
							</div>
						)}
						{results.map((result) => {
							const quizzerBadge = getQuizzerBadge(result.gamesPlayed);
							const creatorBadge = getCreatorBadge(result.quizPlays);
							return (
								<button
									key={result.userId}
									type="button"
									onMouseDown={(event) => event.preventDefault()}
									onClick={() => handleSelect(result.userId)}
									className="flex w-full items-center gap-3 px-4 py-2 text-left hover:bg-accent"
								>
									<Avatar className="h-8 w-8">
										<AvatarFallback className="bg-primary/10 text-primary">
											{getInitials(result.username)}
										</AvatarFallback>
									</Avatar>
									<div className="flex flex-col">
										<span className="text-sm font-medium">{result.username}</span>
										<div className="mt-1 flex flex-wrap gap-1">
											<Badge variant="secondary" className="px-1.5 py-0 text-[10px] uppercase">
												Quizzer: {quizzerBadge.level}
											</Badge>
											<Badge variant="outline" className="px-1.5 py-0 text-[10px] uppercase">
												Creator: {creatorBadge.level}
											</Badge>
										</div>
									</div>
								</button>
							);
						})}
					</div>
				</div>
			)}
		</div>
	);
}

function getInitials(username: string) {
	return username
		.split(" ")
		.filter(Boolean)
		.map((n) => n[0])
		.join("")
		.slice(0, 2)
		.toUpperCase();
}
