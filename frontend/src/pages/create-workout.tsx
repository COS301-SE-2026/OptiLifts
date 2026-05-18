import { useState } from 'react'
import { Plus, Dumbbell } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
	Card,
	CardContent,
	CardHeader,
	CardTitle,
} from '@/components/ui/card'
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuItem,
	DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { PageTitle } from '@/components/ui/page-title'
import { SearchInput } from '@/components/ui/search-input'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'

const RECOMMENDED_EXERCISES = [
	{ name: 'Bicep curl', muscleGroup: 'Biceps' },
	{ name: 'Tricep pushdown', muscleGroup: 'Triceps' },
	{ name: 'Lat pulldown', muscleGroup: 'Lats' },
] as const

const MUSCLE_OPTIONS = ['All Muscles', 'Biceps', 'Triceps', 'Lats', 'Hamstrings', 'Chest', 'Shoulders'] as const
const EQUIPMENT_OPTIONS = ['All Equipment', 'Dumbbell', 'Barbell', 'Cable', 'Machine', 'Bodyweight'] as const

    function addExercise(name: string) {
		console.log('Add exercise:', name)
	}
	function refreshRecommended() {
		console.log('Refresh recommended exercises')
	}

export default function CreateWorkoutPage() {
	const [selectedMuscle, setSelectedMuscle] = useState<(typeof MUSCLE_OPTIONS)[number]>('All Muscles')
	const [selectedEquipment, setSelectedEquipment] = useState<(typeof EQUIPMENT_OPTIONS)[number]>('All Equipment')
	const [searchQuery, setSearchQuery] = useState('')


	const filteredRecommended = RECOMMENDED_EXERCISES.filter((exercise) => {
		const q = searchQuery.trim().toLowerCase()
		if (q && !exercise.name.toLowerCase().includes(q) && !exercise.muscleGroup.toLowerCase().includes(q)) return false
		if (selectedMuscle !== 'All Muscles' && exercise.muscleGroup !== selectedMuscle) return false
		return true
	})

	return (
			<section className="w-full px-2 py-6 sm:px-3 lg:px-2 lg:py-6">
				<div className="grid grid-cols-12 gap-1 lg:gap-2">
					<div className="col-span-12 lg:col-span-6 lg:pt-1">
						<PageTitle title="Create Workout" />
					</div>

					<div className="col-span-12 ml-auto mr-6 w-full max-w-none space-y-4 lg:col-span-6 lg:max-w-[320px] lg:space-y-6">
						<Card className="overflow-hidden border-border bg-card">
							<CardHeader className="px-3 pt-1 pb-1 sm:px-4 sm:pt-1 sm:pb-1">
								<div className="flex items-center justify-between gap-3">
								<CardTitle className="text-left text-base font-bold text-foreground">Recommended</CardTitle>
									<Button
										type="button"
										variant="text"
										className="h-auto p-0 text-xs font-semibold normal-case tracking-normal text-brand hover:text-brand-2"
										onClick={refreshRecommended}
									>
										Refresh
									</Button>
								</div>
							</CardHeader>

							<CardContent className="px-0 pb-0">
								<div className="divide-y divide-border/70">
							{filteredRecommended.length > 0 ? filteredRecommended.map((exercise) => (
								<div key={exercise.name} className="flex items-center gap-3 px-3 py-2 sm:px-4 sm:py-2.5 rounded-md">
									<button
										type="button"
										className="min-w-0 flex-1 text-left focus:outline-none focus-visible:ring-2 focus-visible:ring-brand rounded-md"
										onClick={() => addExercise(exercise.name)}
										aria-label={`Add ${exercise.name} (row)`}
									>
										<div className="flex items-center gap-3">
											<Avatar className="size-9 shrink-0 border border-border sm:size-10">
												<AvatarFallback className="bg-surface-2">
													<Dumbbell className="size-4 text-muted-foreground" />
												</AvatarFallback>
											</Avatar>
											<div className="min-w-0 flex-1">
												<div className="truncate text-sm font-semibold text-foreground">{exercise.name}</div>
												<div className="mt-0.5 text-xs text-muted-foreground">{exercise.muscleGroup}</div>
											</div>
										</div>
									</button>

									<Button
										type="button"
										variant="icon"
										size="icon"
										aria-label={`Add ${exercise.name}`}
										onClick={() => addExercise(exercise.name)}
										className="size-6 rounded-md border-border bg-surface-2 text-foreground hover:bg-border"
									>
										<Plus size={12} />
									</Button>
								</div>
							)) : (
								<div className="px-3 py-3 text-sm text-muted-foreground">No recommended exercises match your search.</div>
							)}
								</div>
							</CardContent>
						</Card>

						<Card className="overflow-hidden border-border bg-card">
							<CardHeader className="px-3 pt-1 pb-1 sm:px-4 sm:pt-1 sm:pb-1">
								<div className="flex items-center justify-between gap-3">
									<CardTitle className="text-left text-base font-bold text-foreground">Exercises</CardTitle>
									<Button
										type="button"
										variant="text"
										className="h-auto p-0 text-xs font-semibold normal-case tracking-normal text-brand hover:text-brand-2"
									>
										+ Create Exercise
									</Button>
								</div>
							</CardHeader>

							<CardContent className="space-y-2 px-3 pb-2 sm:space-y-2 sm:px-4 sm:pb-3">
								<DropdownMenu>
									<DropdownMenuTrigger
										variant="filter"
										className="w-full rounded-md border border-border bg-background px-3 py-2 text-left text-sm font-medium text-foreground shadow-none"
									>
										<span>{selectedMuscle}</span>
									</DropdownMenuTrigger>
									<DropdownMenuContent className="w-[--radix-dropdown-menu-trigger-width]">
										{MUSCLE_OPTIONS.map((option) => (
											<DropdownMenuItem key={option} onSelect={() => setSelectedMuscle(option)}>
												{option}
											</DropdownMenuItem>
										))}
									</DropdownMenuContent>
								</DropdownMenu>

								<DropdownMenu>
									<DropdownMenuTrigger
										variant="filter"
										className="w-full rounded-md border border-border bg-background px-3 py-2 text-left text-sm font-medium text-foreground shadow-none"
									>
										<span>{selectedEquipment}</span>
									</DropdownMenuTrigger>
									<DropdownMenuContent className="w-[--radix-dropdown-menu-trigger-width]">
										{EQUIPMENT_OPTIONS.map((option) => (
											<DropdownMenuItem key={option} onSelect={() => setSelectedEquipment(option)}>
												{option}
											</DropdownMenuItem>
										))}
									</DropdownMenuContent>
								</DropdownMenu>

								<SearchInput
									value={searchQuery}
									onChange={(event) => setSearchQuery(event.target.value)}
									placeholder="Search"
									aria-label="Search exercises"
									className="h-8 rounded-md border border-border bg-background pr-10 text-sm text-foreground placeholder:text-muted-foreground"
								/>
							</CardContent>
						</Card>
					</div>
				</div>
			</section>
	)
}
