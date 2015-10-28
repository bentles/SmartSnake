// Generate normally-distributed random nubmers
// Algorithm adapted from:
// http://c-faq.com/lib/gaussian.html
function rnorm(mean, stdev) {
  var u1, u2, v1, v2, s;
  if (mean === undefined) {
    mean = 0.0;
  }
  if (stdev === undefined) {
    stdev = 1.0;
  }
  if (rnorm.v2 === null) {
    do {
      u1 = Math.random();
      u2 = Math.random();

      v1 = 2 * u1 - 1;
      v2 = 2 * u2 - 1;
      s = v1 * v1 + v2 * v2;
    } while (s === 0 || s >= 1);

    rnorm.v2 = v2 * Math.sqrt(-2 * Math.log(s) / s);
    return stdev * v1 * Math.sqrt(-2 * Math.log(s) / s) + mean;
  }

  v2 = rnorm.v2;
  rnorm.v2 = null;
  return stdev * v2 + mean;
}


function snake_game_builder(width, height, start_length, vision, nnet)
{    
    //direction consts
    var N = 0;
    var E = 1;
    var S = 2;
    var W = 3;

    //map constants
    var WALL = -1;
    var EMPTY = 0;

    //entity consts
    var SNAKE = -2;
    var APPLE = 1;
    var NONE = 0;
    var DEAD = 666; //used to record when a snake hits itself

    //timer
    var interval_id;

    //death 0.6
    var DEATH = 0.55;

    //eyesight
    var SNAKEVISION = vision;

    var map_tokens = {};
    map_tokens[EMPTY] =".";
    map_tokens[WALL] = "W";
    
    var entity_tokens = {};
    entity_tokens[SNAKE] ="S";
    entity_tokens[APPLE] ="A";
    entity_tokens[DEAD] ="X";
    
    var board = [];
    var snake = [];
    var apple_pos ;
    var direction = N;
    var score = 0;
    var lived = 0;
    var HP = width*height*DEATH;
    var time = 0;

    //snake can remember the last 5 things it did
    var snakemem = [];

    //a seperate map of all entities for fast checking and drawing
    var entity_map = {};

    function board_set(x, y, value) {
        board[x + y*width] = value;
    }

    function board_get(x, y, value) {
        if (x < 0 || x >= width || y >= height || y < 0)
            return WALL;
        else {
            return board[x + y*width];
        }
    }

    function build_board() {
        //make some borders around the edge
        for(var i = 0; i < width; i++) {
            for(var j = 0; j < height; j++) {
                if (i === 0 || i === (width - 1) || j === 0 || j === (height - 1)) {
                    board_set(i, j, WALL);
                }
                else {
                    board_set(i, j , EMPTY);
                }
            }
        }
    }

    function get_id(x,y) {
        return x + y*width;
    }

    //assume snake length isn't stupidly long
    function build_snake() {
        var start_pos = {x:Math.floor(width/2), y:Math.floor(height/2)};
        
        for(var i = 0; i < start_length; i++) {
            snake[i] = {x:start_pos.x,y:start_pos.y + i};
            entity_map[get_id(snake[i].x, snake[i].y)] = SNAKE;
        }
    }

    function is_snake(x, y) {
        return entity_map[get_id(x,y)] === SNAKE;
    }

    function get_entity(x, y) {
        return entity_map[get_id(x,y)] || NONE;
    }

    //returns the blocks in lines of distance length to the left right
    //and in front of the snake
    function get_collidible_neighbours(distance) {
        distance = distance || 1;

        var forward = {x:0, y:0};
        
        if (direction == N) 
            forward.y = -1;        
        else if (direction == S) 
            forward.y = 1;        
        else if (direction == E) 
            forward.x = 1;        
        else 
            forward.x = -1;        

        var left = get_left(forward);
        var right = get_right(forward);

        var collidables = [];

        for(var i = 1; i <= distance; i++) {
            var fwd_pos = add(snake[0], mul(forward, i));
            var fwd_block = get_entity(fwd_pos.x, fwd_pos.y) || board_get(fwd_pos.x, fwd_pos.y);
            var left_pos = add(snake[0], mul(left, i));
            var left_block = get_entity(left_pos.x, left_pos.y) || board_get(left_pos.x, left_pos.y);
            var right_pos = add(snake[0], mul(right, i));
            var right_block = get_entity(right_pos.x, right_pos.y) || board_get(right_pos.x, right_pos.y);
                    
            collidables.push(fwd_block);
            collidables.push(left_block);
            collidables.push(right_block);
        }
        
	//console.log(collidables);
        return collidables;
    }

    function mul(a, k)
    {
        var c = {};
        c.x = a.x * k;
        c.y = a.y * k;
        return c;
    }

    function add(a, b)
    {
        var c = {};
        c.x = a.x + b.x;
        c.y = a.y + b.y;

        return c;
    }

    function get_left(forward)
    {
        var left = {};
        left.x = -forward.y;
        left.y = forward.x;
        return left;
    }

    function get_right(forward)
    {
        var right = {};
        right.x = forward.y;
        right.y = -forward.x;
        return right;
    }

    function move_grow_snake() {
        //where are we headed
        var offset = next_pos_offset();
        
        //add an extra snakey to where the end will be if there's an apple
        var grew = entity_map[get_id(snake[0].x + offset.x, snake[0].y + offset.y)] === APPLE ;
        var old_length = snake.length;
        var tail = snake[snake.length - 1];
        if (grew) {
            var length = 
            snake[old_length] = {};
            snake[old_length].x = tail.x;
            snake[old_length].y = tail.y;
        }
        else {
            //update entity map because tail has moved on           
            //delete is supposed to be slow for reasons so this will do
            //the map can only ever be about as big as the board 
            entity_map[get_id(tail.x, tail.y)] = undefined;
        }
        
        //move everything we know up one
        //dont move new part of the snake
        for(var i = old_length -1; i >= 1 ; i--) {
            snake[i].x = snake[i-1].x;
            snake[i].y = snake[i-1].y;
        }

        var head_new_x = snake[0].x + offset.x;
        var head_new_y = snake[0].y + offset.y;
        
        //move the head of the snake
        if (is_snake(head_new_x, head_new_y)) {
            entity_map[get_id(head_new_x, head_new_y)] = DEAD;
        }
        else {
            entity_map[get_id(head_new_x, head_new_y)] = SNAKE;
        }
        
        snake[0].x = head_new_x;
        snake[0].y = head_new_y;
        return grew;
    }

    function next_pos_offset()
    {
        var offset = {x:0, y:0};

        if (direction == N)
            offset.y--;
        else if (direction == S)
            offset.y++;
        else if (direction == E)
            offset.x++;
        else 
            offset.x--;

        return offset;
    }

    function is_dead() {
        //hit a wall
        if (board_get(snake[0].x, snake[0].y) < 0)
            return true;

        //hit self
        return get_entity(snake[0].x, snake[0].y) === DEAD;
    }

    function inc_score() {
        score += 100;
        lived += (DEATH * width * height) - HP;
    }

    function get_input()
    {
        //this calculates the new heading
        var neighbours = get_collidible_neighbours(SNAKEVISION);

        var inputs = [];
        for(var i = 0; i < neighbours.length; i++) {
            var k = i * 4;
            //nothing
            inputs[k] = neighbours[i] == NONE ? 1 : 0;
            //apple
            inputs[k + 1] = neighbours[i] == APPLE ? 1 : 0;
            //wall
            inputs[k + 2] = neighbours[i] == WALL ? 1 : 0;
            //self
            inputs[k + 3] = neighbours[i] == SNAKE ? 1 : 0;
        }

        //inputs = inputs.concat(snakemem/*.slice(6, 15)*/);

        //console.log(inputs);
        
        var sx = snake[0].x;
        var sy = snake[0].y;
        var ax = apple_pos[0];
        var ay = apple_pos[1];
        var rel_apple_pos = [0,0,0,0];

        if (direction === N)        
            rel_apple_pos = [ ax - sx, ay - sy, 0, 0];        
        else if (direction === S)        
            rel_apple_pos = [ ay - sy, -(ax - sx), 0, 0];        
        else if (direction === W)        
            rel_apple_pos = [ -(ay - sy), ax - sx, 0, 0 ];        
        else        
            rel_apple_pos = [ -(ax - sx), -(ay - sy), 0, 0];

	if (rel_apple_pos[0] < 0)
        {
            rel_apple_pos[2] = -rel_apple_pos[0];
            rel_apple_pos[0] = 0;
        }

        if (rel_apple_pos[1] < 0)
        {
            rel_apple_pos[3] = -rel_apple_pos[1];
            rel_apple_pos[1] = 0;
        }
        inputs = inputs.concat(rel_apple_pos);
     //   inputs.push(snake.length);
//console.log(inputs);
        
        var dir = nnet.get_output(inputs);
        var max = 0;
        var max_pos = 0;
        for(var i = 0; i < dir.length; i++) {
            if (dir[i] > max) {
                max = direction[i];
                max_pos = i;
            }                
        }

        snakemem = snakemem.slice(3,15);
        //l
        snakemem.push(max_pos == 0 ? 1 : 0);
        //f
        snakemem.push(max_pos == 1 ? 1 : 0);
        //r
        snakemem.push(max_pos == 2 ? 1 : 0);
        
        
        return (direction + max_pos + 3) % 4;
    }

    function time_step() {
        direction = get_input();
        var grew = move_grow_snake();
        if(grew) {
            inc_score();
            apple_pos = add_apple();
            HP = width * height * DEATH;
        }
        HP--;
        time++;
        return HP <= 0 ? true : is_dead();
    }

    function pretty_print()
    {
        clear();
        for(var j = 0; j < height; j++) {
            var line = "";
            for(var i = 0; i < width; i++) {
                var char = map_tokens[board_get(i,j)];
                if (get_entity(i,j) !== NONE ) {
                    char = entity_tokens[get_entity(i,j)];
                }
                line += char;
            }                
            console.log(line);                
        }
        console.log("SCORE: " + score);
        console.log(snakemem);
    }

    function clear() {
        process.stdout.write('\u001B[2J\u001B[0;0f');
    }

    //will probably break if snake is massive, will have to reimplement then
    function add_apple() {
        var size = width*height;
        
        while(true) {
            var pos = Math.floor(Math.random()*size);
            var x = pos % width;
            var y = Math.floor(pos / width);
            if (!is_snake(x, y) && (board_get(x, y) === EMPTY)) {
                entity_map[pos] = APPLE;
                break; //stop trying - found a valid spot for an apple
            }
        }

        return [x,y];
    }

    function game() {
        var done = !game_loop();
        pretty_print();
        //stop game if snake dies
        if (done) {
        clearInterval(interval_id);
            console.log('GAME OVER');
        }
    }

    function game_loop() {
        var end = time_step();
        return !end;
    }

    function play(milliseconds) {
        init();
        interval_id = setInterval(game, milliseconds);
    }

    function run()
    {
        init();
        while(game_loop()){}
        return score; ///time; //+ time; //time is constrained
    }

    function init()
    {
        board = [];
        snake = [];
        direction = N;
        score = 0;
        lived = 0;
        HP = width*height*DEATH;
        time = 0;
        entity_map = {};
        snakemem = [0,1,0,0,1,0,0,1,0,0,1,0,0,1,0];
        
        //set up actual game (finally sheesh)
        build_board();
        build_snake();
        apple_pos = add_apple();
    }
    
    
    return {play:play, run:run} ;
}

function FFNN_builder(num_in,
              num_hidden, hidden_func,
                      num_out, out_func) {
    var inputs = [];
    var hidden_neurons = [];
    var out_neurons = [];
    var h_outputs = [];
    var outputs = [];
    var in_to_hidden = {};
    var hidden_to_out = {};
    init();

    //associate a function with each neuron
    function neuron_builder(func) {
        var neuron = {};
        neuron.func = func;

        return neuron;
    }
    
    function init() {
        //set up neuron lists
        //===================
        //input neurons
        var i = 0;
        var j = 0;
        
        for(;i < num_in; i++) {
            inputs[i] = 0;
        }
        //bias for hiddens
        inputs[i] = -1;
        
        //hidden neurons
        for(i = 0; i < num_hidden; i++) {
            hidden_neurons[i] = neuron_builder(hidden_func);
        }
        //bias for outputs
        hidden_neurons[i] = neuron_builder(function(){return -1;});

        //output neurons
        for(i = 0; i < num_out; i++) {
            out_neurons[i] = neuron_builder(out_func);
        }

        //set up weights as maps
        //=====================
        //in to hidden
        for(i = 0; i < inputs.length; i++) { //include bias
            for(j = 0; j < hidden_neurons.length - 1; j++) { //exclude bias
                in_to_hidden[i + "," + j] = Math.random() / 10 ; //random small weights
            }            
        }
        //hidden to out
        for(i = 0; i < hidden_neurons.length; i++) { //include bias
            for(j = 0; j < out_neurons.length; j++) { 
                hidden_to_out[i + "," + j] = Math.random() / 10 ; //random small weights
            }            
        }
    }

    function set_weight(i, j, map, weight) {
        map[i + "," + j] = weight;
    }

    function set_all_weights(weightlist) {
        //set weights in a set order
        //sort the key lists first to get a set order
        
        var i2h_keys = Object.keys(in_to_hidden).sort();
        var i = 0;
        for(i = 0; i < i2h_keys.length; i++) {
            in_to_hidden[i2h_keys[i]] = weightlist[i] ;
        }

        var h2o_keys = Object.keys(hidden_to_out).sort();
        for(var j = 0; j < h2o_keys.length; j++) {
            hidden_to_out[h2o_keys[j]] = weightlist[i + j];
        }
    }

    function get_num_weights() {
        return Object.keys(in_to_hidden).length +
            Object.keys(hidden_to_out).length;
    }

    function get_weight(i,j,map) {
        return map[i + "," + j];
    }

    //takes an array of inputs
    function feed_forward(ins) {
        for(var i = 0; i < ins.length; i++) {
            inputs[i] = ins[i];
        }
        
        var j = 0;
        for(; j < hidden_neurons.length - 1; j++) { //excludes bias
            var net = 0;
            for(var i = 0; i < inputs.length; i++) { //includes bias
                net += inputs[i]*get_weight(i,j, in_to_hidden);
            }
            h_outputs[j] = hidden_neurons[j].func(net);
        }        
        //get bias output
        h_outputs[j] = hidden_neurons[j].func();


        var j = 0;
        for(; j < out_neurons.length; j++) { 
            var net = 0;
            for(var i = 0; i < h_outputs.length; i++) { //includes bias
                net += h_outputs[i]*get_weight(i,j, hidden_to_out);
            }
            outputs[j] = out_neurons[j].func(net);
        }
        return outputs;
    }

    return {set:set_all_weights, get_output:feed_forward, size:get_num_weights};
}

function GA_builder(pop_size, tourn_percent, std_dev, mutate_chance) {
    var vision = 2;
    var nnet = FFNN_builder( /*5 * 3*/ 12 * vision + 4,
                            6, function(net){return net;},
                            3, function(net){return net;});
    var game = snake_game_builder(20, 10, 3, vision, nnet);
    var chromosomes = [];
    var max_fit = -Number.MAX_VALUE;
    var max_fit_chromo = [];
    var fitnesses = [];
    var fitness_stagnation = 500;
    var fitness_stagnation_count = fitness_stagnation;

    function create_first_gen() {
        var chromosome_size = nnet.size();
        for(var i = 0; i < pop_size; i++) {
            var new_chrom = [];
            for(var j = 0; j < chromosome_size; j++) {
                new_chrom[j] = (Math.random() - 0.5) / 2;
            }
            chromosomes[i] = new_chrom;
        }
    }

    function calc_fitnesses()
    {
        var cont = true;
        var new_max = false;
        
        for(var i = 0; i < chromosomes.length; i++) {
            nnet.set(chromosomes[i]);

           
            var score = 0;
            
            var num_runs = 5;            
            for(var j = 0; j < num_runs; j++) {
                score += game.run();                
            }

            fitnesses[i] = score / num_runs;            

            if (fitnesses[i] > max_fit) {
                fitness_stagnation_count = fitness_stagnation;
                
                max_fit = fitnesses[i];
                max_fit_chromo = chromosomes[i];
                console.log("NEW MAX FITNESS! " + fitnesses[i]);
                new_max = true;
            }
        }

        //no new max found for fitness_stagnation gens then we give up
        if (!new_max) {
            fitness_stagnation_count--;
            if (!fitness_stagnation_count)            
                cont = false;
            
        }

        return cont;
    }

    function tourn_select() {
        var size = Math.floor(chromosomes.length * tourn_percent);
        var selections = [];
        for(var i = 0; i < size; i++) {
            selections[i] = Math.floor(Math.random()*chromosomes.length);
        }


        var m_fit = -Number.MAX_VALUE;
        var m_fit_pos = 0;
        for(var i = 0; i < selections.length; i++) {
            if (fitnesses[selections[i]] > m_fit)
            {
                m_fit = fitnesses[selections[i]];
                m_fit_pos = selections[i];
            }
        }

        return m_fit_pos;
    }

    function next_gen()
    {
        var children = [];
        var cont = calc_fitnesses();

        while(children.length < chromosomes.length) {
            var p1 = tourn_select();
            var p2 = tourn_select();
            var siblings = crossover_mutate(p1, p2);
            children = children.concat(siblings);           
        }
        chromosomes = children;

        return cont; //propagate signal to kill GA because it's not learning anything
    }

    function crossover_mutate(p1, p2) {
        var parent1 = chromosomes[p1];
        var parent2 = chromosomes[p2];
        var child1 = [];
        var child2 = [];

        for(var i = 0; i < parent1.length; i++) {
            var swap = Math.random() > 0.5;
            var r1 = rnorm(0, std_dev);
            var r2 = rnorm(0, std_dev);
            var mutate_c1 = Math.random() < mutate_chance ? r1 : 0;
            var mutate_c2 = Math.random() < mutate_chance ? r2 : 0;
 
            if (swap) {
                child1[i] = parent2[i] + mutate_c1;
                child2[i] = parent1[i] + mutate_c2;
            }
            else {
                child1[i] = parent1[i] + mutate_c1;
                child2[i] = parent2[i] + mutate_c2;
            }            
        }

        return [child1, child2];
    }

    function evolve(iter) {
        var totalgens = iter;
        create_first_gen();

        while(next_gen() && iter--) {            
            if (iter % 100 == 0) {
                console.log("NEW GEN (" + (totalgens - iter)  + "): " + max_fit);
            }
        }
        return max_fit_chromo;
    }

    function play_best(dt)
    {
        nnet.set(max_fit_chromo);
        game.play(dt);
    }

    return { evolve:evolve, play_best:play_best };
}

//20 0.5 2.5 0.01
var ga = GA_builder(20, 0.5, 2.5, 0.005); //pop_size, tourn_percent, std_dev, mutate_chance
var chromo = ga.evolve(3000);
ga.play_best(50);
