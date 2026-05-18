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


function addExercise(name: string) {
		console.log('Add exercise:', name)
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

								</div>
							</CardHeader>
                        </Card>


						<Card className="overflow-hidden border-border bg-card">
							<CardHeader className="px-3 pt-1 pb-1 sm:px-4 sm:pt-1 sm:pb-1">
								<div className="flex items-center justify-between gap-3">
									<CardTitle className="text-left text-base font-bold text-foreground">Exercises</CardTitle>

								</div>
							</CardHeader>

							<CardContent className="space-y-2 px-3 pb-2 sm:space-y-2 sm:px-4 sm:pb-3">
							</CardContent>
						</Card>
					</div>
				</div>
			</section>
	)
}
